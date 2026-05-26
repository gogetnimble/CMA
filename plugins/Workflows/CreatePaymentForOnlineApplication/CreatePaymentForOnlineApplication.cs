using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using System.Net.Http;

namespace CreatePaymentForOnlineApplication
{
    /// <summary>
    /// Create a payment for an online application 
    /// Used to create a paymnet when check received flag is set in the application record
    /// 
    /// Conditions:
    ///     Application Pay status need to be Payment Suceeeded, Deferred to PTMA or Ot Required 
    ///     If Application Pay type is check Check received needs to be true
    ///     Applictaion amount must be > 0 
    ///     Application must have a batch with an order associated to it. 
    ///     The order must not have a paymenty associated
    /// 
    /// </summary>
    public class CreatePaymentForOnlineApplication : CodeActivity
    {

        protected override void Execute(CodeActivityContext context)
        {
            // Obtain the organization service reference.
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService ts = context.GetExtension<ITracingService>();

            EntityReference appRef = new EntityReference("new_onlineapplication", workflowContext.PrimaryEntityId);

            //read valus from online application, contact and account
            Entity application = service.Retrieve(appRef.LogicalName, appRef.Id, new ColumnSet("new_name", "new_parentcustomerid", "new_paymentmethod", "new_paymentstatus", "new_chequerecieved", "new_grandtotal"));

            EntityReference currency = CrmHelpers.SearchForRef(service, "transactioncurrency", "isocurrencycode", "CAD");

            string applicationName = application.GetAttributeValueEx<string>("new_name", ts, "app.");
            EntityReference contactRef = application.GetAttributeValueEx<EntityReference>("new_parentcustomerid", ts, "app.");
            OptionSetValue paymentMethod = application.GetAttributeValueEx<OptionSetValue>("new_paymentmethod", ts, "app.");
            OptionSetValue paymentStatus = application.GetAttributeValueEx<OptionSetValue>("new_paymentstatus", ts, "app.");
            OptionSetValue chequeReceived = application.GetAttributeValueEx<OptionSetValue>("new_chequerecieved", ts, "app.");
            Money totalAmount = application.GetAttributeValueEx<Money>("new_grandtotal", ts, "app.");

            //check for previous batch/paymnet/order this application 
            string fetchXML = $@"<fetch>
                                    <entity name='new_batch' >
                                    <attribute name='new_batchid' />
                                    <filter>
                                        <condition attribute='new_onlineapplicationid' operator='eq' value='{application.Id}' />
                                    </filter>
                                    <link-entity name='salesorder' from='new_batchid' to='new_batchid' link-type='outer' alias='order' >
                                        <attribute name='name' />
                                        <attribute name='salesorderid' />
                                          <link-entity name='new_payment' from='new_orderid' to='salesorderid' link-type='outer' alias='payment' >
                                            <attribute name='new_paymentid' />
                                          </link-entity>
                                    </link-entity>
                                    </entity>
                                </fetch>";
            ts.Trace($"FetchXML: {fetchXML}");
            Entity batch = CrmHelpers.GetFirstFromExecuteFetch(service, fetchXML);
            string orderName = batch != null ? batch.GetAttributeValueEx<string>("order.name", ts) : null;
            EntityReference orderRef = batch != null ? new EntityReference("salesorder", batch.GetAttributeValueEx<Guid>("order.salesorderid", ts)) : null;
            Guid paymentId = batch != null ? batch.GetAttributeValueEx<Guid>("payment.new_paymentid", ts) : Guid.Empty;

            if (contactRef == null)
                ts.Trace($"No contact set in application");
            else if (batch == null)
                ts.Trace($"No batch exists for Online application '{applicationName}'");
            else if (paymentId != Guid.Empty)
                ts.Trace($"Payment already exists");
            else if (paymentMethod?.Value == (int)ApplicationPaymentMethod.Cheque && chequeReceived?.Value != (int)ApplicationChequeReceived.Yes)
                ts.Trace($"Check not received for application");
            else if (paymentStatus?.Value != (int)ApplicationPaymentStatus.Succeeded && paymentStatus?.Value != (int)ApplicationPaymentStatus.PTMA && paymentStatus?.Value != (int)ApplicationPaymentStatus.NotRequired)
                ts.Trace($"Invalid payment status for application");
            else if (totalAmount == null || totalAmount.Value <= 0m)
                ts.Trace($"Invalid totalAmount for application");
            else if (orderRef == null || orderRef.Id == Guid.Empty)
                ts.Trace($"No Order for Application");
            else
            {
                //create a paymnet records
                var payment = new Entity { LogicalName = "new_payment" };
                payment.Attributes["new_orderid"] = orderRef;
                payment.Attributes["new_batchid"] = batch.ToEntityReference();
                payment.Attributes["new_paymentfromcontactid"] = contactRef;
                payment.Attributes["transactioncurrencyid"] = currency;
                payment.Attributes["new_paymentamount"] = totalAmount;
                payment.SetAttributeValue("new_paymentmethod", paymentMethod);
                payment.SetAttributeValue("new_onlineapplicationid", appRef);
                payment.Attributes["statecode"] = new OptionSetValue((int)PaymentState.Active);
                payment.Attributes["statuscode"] = new OptionSetValue((int)PaymentSubStatus.Planned);
                //undo this line - commenting out of service.create
                //https://dev.azure.com/cmaamc/CMA%20CRM/_boards/board/t/Connect%20Team/Stories/?workitem=7587
                //undo above line - 
                //service.Create(payment);
                ts.Trace("Created Payment");
            }

        }
    }

}      
        
