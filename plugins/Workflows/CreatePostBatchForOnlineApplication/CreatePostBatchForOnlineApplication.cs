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
using System.Net;

namespace CreatePostBatchForOnlineApplication
{
    public class CreatePostBatchForOnlineApplication : CodeActivity
    {
        #region Input/Output Arguments        
        #endregion

        protected override void Execute(CodeActivityContext context)
        {
            // Obtain the organization service reference.
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService ts = context.GetExtension<ITracingService>();

            EntityReference appRef = new EntityReference("new_onlineapplication", workflowContext.PrimaryEntityId);

            //read valus from online application, contact and account
            Entity application = service.Retrieve(appRef.LogicalName, appRef.Id, new ColumnSet(true));
            string applicationName = application.GetAttributeValueEx<string>("new_name", ts, "app.");
            EntityReference pricelevel = application.GetAttributeValueEx<EntityReference>("new_membershippricelistid", ts, "app.");
            EntityReference contactRef = application.GetAttributeValueEx<EntityReference>("new_parentcustomerid", ts, "app.");
            EntityReference categoryproductid = application.GetAttributeValueEx<EntityReference>("new_membershipcategoryid", ts, "app.");
            string membershipyear = application.GetAttributeValueEx<string>("new_membershipyear", ts, "app.");
            DateTime? expirydate = application?.GetAttributeValueEx<DateTime?>("new_membershipexpirydate", ts, "app.");

            //introduced tax calculation fields starting in January 2024
            Money totalAmount = application.GetAttributeValueEx<Money>("new_grandtotal", ts, "app.");
            Money tax1 = application.GetAttributeValueEx<Money>("new_tax1", ts, "app.");
            Money tax2 = application.GetAttributeValueEx<Money>("new_tax2", ts, "app.");
            Money tax3 = application.GetAttributeValueEx<Money>("new_tax3", ts, "app.");
            decimal tax1percentage = application.GetAttributeValueEx<decimal>("new_tax1_percentage", ts, "app.");
            decimal tax2percentage = application.GetAttributeValueEx<decimal>("new_tax2_percentage", ts, "app.");
            decimal tax3percentage = application.GetAttributeValueEx<decimal>("new_tax3_percentage", ts, "app.");

            string extTransNumber = application.GetAttributeValueEx<string>("new_externalpartytransaction_id", ts, "app.");
            Money prorationDiscount = application.GetAttributeValueEx<Money>("new_prorationdiscountamount", ts, "app.");
            Money matPatDiscount = application.GetAttributeValueEx<Money>("new_matpatleavediscountamount", ts, "app.");
            Money otherDiscount = application.GetAttributeValueEx<Money>("new_promotionalormanualdiscountamount", ts, "app.");

            OptionSetValue paymentMethod = application.GetAttributeValueEx<OptionSetValue>("new_paymentmethod", ts, "app.");
            OptionSetValue chequeReceived = application.GetAttributeValueEx<OptionSetValue>("new_chequerecieved", ts, "app.");
            
            //check for previous batch for this application 
            string fetchXML = $@"<fetch>
                                  <entity name='new_batch' >
                                    <attribute name='new_batchid' />
                                    <filter>
                                      <condition attribute='new_onlineapplicationid' operator='eq' value='{application.Id}' />
                                    </filter>
                                  </entity>
                                </fetch>";
            if( CmaHelpers.OnlineApplicationHasBatch( service, application.Id )) 
                throw new Exception($"A Batch has already been created for Online Application '{applicationName}'");

            //get the CMA online PTMA account
            Entity account = null; 
            EntityReference ptmaRef = application.GetAttributeValueEx<EntityReference>("new_ptmaid", ts, "app.");
            if (ptmaRef != null)
                account = service.Retrieve("account", ptmaRef.Id, new ColumnSet("new_ptmacode"));
            if( account == null)
                throw new Exception($"CreatePostBatchForOnlineApplication() no PTMA account Found'");
            string ptmaCode = account.GetAttributeValueEx<string>("new_ptmacode", ts, "account.");

            if( contactRef == null )
                throw new Exception($"CreatePostBatchForOnlineApplication() no contact set for Online application '{applicationName}'");
            Entity contact = service.Retrieve("contact", contactRef.Id, new ColumnSet("fullname", "lastname", "firstname"));            
            string firstName = application.GetAttributeValueEx<string>("firstname", ts, "contact.");
            string lastName = application.GetAttributeValueEx<string>("lastname", ts, "contact.");
            string fullName = application.GetAttributeValueEx<string>("fullname", ts, "contact.");

            //do some simple vaidation 
            if (pricelevel == null)
                throw new Exception($"Error no Membership pricelist found in Online Application '{applicationName}'");
            int year = 0;
            Int32.TryParse(membershipyear, out year);
            if( year < DateTime.Now.Year)
                throw new Exception($"Invalid or missing Membership year in Online Application '{applicationName}' ");

            //create a new batch 
            Entity newBatch = new Entity("new_batch");
            string batchName = CmaHelpers.GetBatchId(service, year, ptmaCode);
            newBatch.SetAttributeValue("new_name", batchName);
            newBatch.SetAttributeValue("new_divassocaccountid", account.ToEntityReference());
            newBatch.SetAttributeValue("new_year", year.ToString());
            newBatch.SetAttributeValue("new_pricelist", pricelevel);
            newBatch.SetAttributeValue("new_onlineapplicationid", application.ToEntityReference()  );
            Guid newBatchId = service.Create(newBatch);
            EntityReference batchRef = new EntityReference("new_batch", newBatchId);
            ts.Trace($"Created Batch : {batchName}");

            //create a transaction 
            int transNumber = 1;
            Entity newTrans = new Entity("new_transaction");
            //set contact ptma, expiry, btach Id, year 
            newTrans.SetAttributeValue("new_batchid", batchRef);
            newTrans.SetAttributeValue("new_transaction_id_number", transNumber);
            newTrans.SetAttributeValue("new_name", $"{batchName} - {transNumber}");
            newTrans.SetAttributeValue("new_contactid", contactRef);
            newTrans.SetAttributeValue("new_firstname", firstName);
            newTrans.SetAttributeValue("new_lastname", lastName);

            //for application payment type check only set amount create if cheque received or payment type not Ptma 
            decimal payAmount = 0;
            if ((paymentMethod?.Value == (int)ApplicationPaymentMethod.Cheque && chequeReceived?.Value == (int)ApplicationChequeReceived.Yes) ||
                 (paymentMethod?.Value != (int)ApplicationPaymentMethod.PTMA && paymentMethod?.Value != (int)ApplicationPaymentMethod.Cheque))
                payAmount = totalAmount != null ? totalAmount.Value : 0;

            //introduced tax calculation fields starting in January 2024
            newTrans.SetAttributeValue("new_amountpaid", new Money( payAmount ));
            newTrans.SetAttributeValue("new_tax1", (tax1));
            newTrans.SetAttributeValue("new_tax2", (tax2));
            newTrans.SetAttributeValue("new_tax3", (tax3));
            newTrans.SetAttributeValue("new_tax1_percentage", (tax1percentage));
            newTrans.SetAttributeValue("new_tax2_percentage", (tax2percentage));
            newTrans.SetAttributeValue("new_tax3_percentage", (tax3percentage));

            newTrans.SetAttributeValue("new_quantity", new decimal(1.0));
            newTrans.SetAttributeValue("new_categoryproductid", categoryproductid);
            newTrans.SetAttributeValue("new_provincialmembercategory", CmaHelpers.GetMembershipXref(service, categoryproductid.Id, account.Id));
            newTrans.SetAttributeValue("new_onlinelineapplicationid", application.ToEntityReference());
            newTrans.SetAttributeValue("new_externalpartytransaction_id", extTransNumber );
            newTrans.SetAttributeValue("new_taxjurisdiction", application.GetAttributeValueEx<OptionSetValue>("new_provincestatecode", ts, "app."));
            if (expirydate.HasValue)
                newTrans.SetAttributeValue("new_expirydate", expirydate.Value);

            //figure out if discount catgory 
            //Note that only one can be selected so first non zero value is used. Order evaluated is promo, proration, matpat
            OptionSetValue discountCategory = null;
            if (otherDiscount?.Value > 0)
                discountCategory = new OptionSetValue((int)DiscountCategory.PromotionalManual);
            else if (prorationDiscount?.Value > 0)
                discountCategory = new OptionSetValue((int)DiscountCategory.Proration);
            else if (matPatDiscount?.Value > 0)
                discountCategory = new OptionSetValue((int)DiscountCategory.MaternityPaternityLeave);
            if (discountCategory != null )
                newTrans.SetAttributeValue("new_discountcategory", discountCategory );
            

            Guid newTransId = service.Create(newTrans);
            ts.Trace($"Added Transaction for Contact: {fullName}({contact.Id})");

            string baseURL = CmaHelpers.GetConfigLookupValue(service, "Azure/ValidateAndPostBatchFunctionBaseURL");
            
            //call Post Function
            string url = baseURL + $"&batchid={batchRef.Id}";
            url += $"&mintxn={1}&maxtxn={20000000}&applicationid={application.Id}";
            ts.Trace($"URL: {url}");
            HttpClient client = new HttpClient();
            client.PostAsync(url, null);

        }
        

    }

}      
        
