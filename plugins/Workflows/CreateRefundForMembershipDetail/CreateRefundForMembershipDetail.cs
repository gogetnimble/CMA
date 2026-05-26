using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Workflow;
using System.Net.Http;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CreateRefundForMembershipDetail
{  
    /// </summary>
    public class CreateRefundForMembershipDetail : CodeActivity
    {
        #region API Classes
        [DataContract]
        public class CMAAPIError
        {

            [DataMember]
            public string code { get; set; }

            [DataMember]
            public string category { get; set; }

            [DataMember]
            public string message { get; set; }

            [DataMember]
            public string reference { get; set; }
        }
        public class CMAAPIRefundResult
        {

            [DataMember]
            public string id { get; set; }

            [DataMember]
            public string authorizing_merchant_id { get; set; }

            [DataMember]
            public string approved { get; set; }

            [DataMember]
            public string message_id { get; set; }

            [DataMember]
            public string message { get; set; }
            [DataMember]
            public string auth_code { get; set; }
        }
        #endregion API Classes
        #region Input/Output Arguments        

        [Input("Product")]
        [RequiredArgument]
        [ReferenceTarget("product")]
        public InArgument<EntityReference> Product { get; set; }

        [Input("Amount")]
        [RequiredArgument]
        public InArgument<Decimal> Amount { get; set; }

        [Output("Result")]
        public OutArgument<string> Result{ get; set; }

        #endregion

        protected override void Execute(CodeActivityContext context)
        {
            // Obtain the organization service reference.
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService ts = context.GetExtension<ITracingService>();
            IExecutionContext localContext = context.GetExtension<IExecutionContext>();
            var userId = localContext.InitiatingUserId;
            
            EntityReference membershipDetailRef = new EntityReference(workflowContext.PrimaryEntityName, workflowContext.PrimaryEntityId);
            EntityReference productRef = Product != null ? Product.Get(context) : null;
            decimal amount = Amount.Get(context);
            ts.Trace($"CMD: {membershipDetailRef.Id} Product:{productRef?.Id}  Amount: {amount}");
            string errorMessage = "";

            string fetchXML = $@"<fetch>
                                  <entity name='new_cmamembershipdetail' >
                                    <attribute name='new_contact' />
                                    <attribute name='new_status' />
                                    <attribute name='new_categoryproductid' />
                                    <attribute name='new_membershipyear' />
                                    <filter>
                                      <condition attribute='new_cmamembershipdetailid' operator='eq' value='{membershipDetailRef.Id}' />
                                    </filter>
                                    <link-entity name='salesorder' from='salesorderid' to='new_orderid' link-type='inner' alias='order' >
                                      <attribute name='salesorderid' />
                                      <attribute name='pricelevelid' />
                                      <attribute name='transactioncurrencyid' />
                                      <link-entity name='new_payment' from='new_orderid' to='salesorderid' link-type='outer' alias='payment' >
                                        <attribute name='new_paymentid' />
                                        <attribute name='new_paymentamount' />
                                        <attribute name='new_creditcardtype' />
                                        <filter>
                                          <condition attribute='new_paymentamount' operator='ge' value='0' />
                                        </filter>
                                        <link-entity name='new_transaction' from='new_transactionid' to='new_transactionid' link-type='outer' alias='transaction' >
                                          <attribute name='new_externalpartytransaction_id' />
                                        </link-entity>
                                      </link-entity>
                                    </link-entity>
                                    <link-entity name='new_cmamembershipdetail' from='new_contact' to='new_contact' link-type='inner' alias='m' >
                                      <attribute name='new_status' />
                                      <attribute name='new_cmamembershipdetailid' />
                                      <attribute name='new_membershipyear' />
                                      <attribute name='new_expirydate' />
                                    </link-entity>
                                  </entity>
                                </fetch>";


            List<Entity> cmds = CrmHelpers.ExecuteFetchEnumerable(service, fetchXML).ToList();
            if (cmds.Count > 0)
            {
                Entity mainCmd = cmds[0];
                Guid paymentId = mainCmd.GetAttributeValueEx<Guid>("payment.new_paymentid", ts, "cmd.");
                Guid orderId = mainCmd.GetAttributeValueEx<Guid>("order.salesorderid", ts, "cmd.");
                string externalTransId = mainCmd.GetAttributeValueEx<string>("transaction.new_externalpartytransaction_id", ts, "cmd.");
                EntityReference contactRef = mainCmd.GetAttributeValueEx<EntityReference>("new_contact", ts, "cmd.");
                EntityReference membershipProductCategoryRef = mainCmd.GetAttributeValueEx<EntityReference>("new_categoryproductid", ts, "cmd.");
                OptionSetValue status = mainCmd.GetAttributeValueEx<OptionSetValue>("new_status", ts, "cmd.");
                Money payAmount = mainCmd.GetAttributeValueEx<Money>("payment.new_paymentamount", ts, "cmd.");
                EntityReference currencyRef = mainCmd.GetAttributeValueEx<EntityReference>("order.transactioncurrencyid", ts, "cmd.");
                OptionSetValue ccType = mainCmd.GetAttributeValueEx<OptionSetValue>("payment.new_creditcardtype", ts, "cmd.");
                EntityReference orderPriceListRef = mainCmd.GetAttributeValueEx<EntityReference>("order.pricelevelid", ts, "cmd.");
                string year = mainCmd.GetAttributeValueEx<string>("new_membershipyear", ts, "cmd.");

                //get config values 
                string refundProductCode = CmaHelpers.GetConfigLookupValue(service, "Membership/RefundProductCode");
                fetchXML = $@"<fetch><entity name='productpricelevel' >
                                    <attribute name='productid' />
                                    <attribute name='uomid' />
                                    <filter><condition attribute='pricelevelid' operator='eq' value='{orderPriceListRef.Id}' /></filter>
                                    <link-entity name='product' from='productid' to='productid' link-type='inner' alias='product' >
                                      <filter><condition attribute='productnumber' operator='eq' value='{refundProductCode}' /></filter>
                                    </link-entity>
                                </entity></fetch>";
                Entity refundProduct = CrmHelpers.GetFirstFromExecuteFetch(service, fetchXML);
                EntityReference refundProductRef = refundProduct?.GetAttributeValueEx<EntityReference>("productid", ts, "productprice.");
                EntityReference refundProductUOMRef = refundProduct?.GetAttributeValueEx<EntityReference>("uomid", ts, "productprice.");

                string discontinueReasonCode = CmaHelpers.GetConfigLookupValue(service, "Membership/RefundDiscontinuedReasonCode");
                fetchXML = $@"<fetch><entity name='new_discontinuedreason' >
                                    <attribute name='new_discontinuedreasonid' />
                                    <attribute name='new_name' />
                                    <filter><condition attribute='new_discontinuedreason_id' operator='eq' value='{discontinueReasonCode}' /></filter>
                                </entity></fetch>";
                EntityReference discontinuedReasonRef = CrmHelpers.GetFirstFromExecuteFetch(service, fetchXML)?.ToEntityReference();



                //test some stuff
                string codeLookupMsg = string.Empty;
                if (status?.Value == (int)CMAMembershipStatus.Discontinued)
                    errorMessage += $"Status of Membership is Discontinued\n";
                if (status?.Value == (int)CMAMembershipStatus.Inactive)
                    errorMessage += $"Status of Membership is Inactive\n";
                if (orderId == Guid.Empty)
                    errorMessage += "No order found for CMA Membership Detail\n";
                if (refundProductRef == null)
                    errorMessage += $"No Refund product found with Product code: {refundProductCode} in PriceList: '{orderPriceListRef.Name}'\n";
                if (paymentId == Guid.Empty)
                { codeLookupMsg = GetErrorMessage(service, userId, "9028");
                    if (string.IsNullOrEmpty(codeLookupMsg))
                        codeLookupMsg = "No Payment found for this Membership order";
                    errorMessage += $"{codeLookupMsg}\n";
                }
                if (amount > payAmount?.Value)
                    errorMessage += $"Refund {amount} exceeds payment amount {payAmount?.Value}\n";
                if (errorMessage == "")
                {
                    Money refundAmount = new Money(0 - amount);
                    EntityReference orderRef = new EntityReference("salesorder", orderId);
                    string refundTransactionId = "";
                    OptionSetValue payMethod = new OptionSetValue((int)PaymentMethod.Cheque);
                    if (!string.IsNullOrWhiteSpace(externalTransId))
                    {
                        payMethod = new OptionSetValue((int)PaymentMethod.CC);
                        string result = CMAAPIRefundPayment(service, amount, externalTransId, ts);
                        if (result.StartsWith("Error"))
                            errorMessage += result + "\n";
                        else
                            refundTransactionId = result;
                    }
                    if (errorMessage == "")
                    {
                        //add an order line for refund 
                        Entity newOrderLine = new Entity("salesorderdetail");
                        newOrderLine.SetAttributeValue("salesorderid", orderRef);
                        newOrderLine.SetAttributeValue("productid", refundProductRef);
                        newOrderLine.SetAttributeValue("uomid", refundProductUOMRef);
                        newOrderLine.SetAttributeValue("quantity", 1.0M);
                        newOrderLine.SetAttributeValue("ispriceoverridden", true);
                        newOrderLine.SetAttributeValue("priceperunit", refundAmount);
                        service.Create(newOrderLine);

                        //create a payment 
                        var payment = new Entity { LogicalName = "new_payment" };
                        payment.SetAttributeValue("new_orderid", orderRef);
                        payment.SetAttributeValue("new_paymentreference", refundTransactionId);
                        payment.SetAttributeValue("new_paymentfromcontactid", contactRef);
                        payment.SetAttributeValue("transactioncurrencyid", currencyRef);
                        payment.SetAttributeValue("new_paymentamount", refundAmount);
                        payment.SetAttributeValue("new_paymentmethod", payMethod);
                        payment.SetAttributeValue("new_creditcardtype", ccType);
                        payment.SetAttributeValue("statuscode", new OptionSetValue((int)PaymentSubStatus.Planned));
                        service.Create(payment);

                        //set up update for  application values 
                        var updateCMD = new Entity { LogicalName = "new_cmamembershipdetail", Id = membershipDetailRef.Id };
                        updateCMD.SetAttributeValue("new_lastrefundamount", new Money( 0 - refundAmount.Value) );
                        updateCMD.SetAttributeValue("new_lastrefundmethod", CrmHelpers.GetOptionSetLabelByValue(service, "new_payment", "new_paymentmethod", payMethod.Value));
                        //update the CMD if required                     
                        if (amount == payAmount.Value || productRef.Id != membershipProductCategoryRef.Id || status?.Value == (int)CMAMembershipStatus.Pending)
                        {
                            bool updateToDiscontinued = false;
                            //IF it's a full refund and the category provided by the user is the same one as the category on the active membership THEN update the membership status to discontinued
                            if (amount == payAmount.Value && productRef?.Id == membershipProductCategoryRef.Id)
                            {
                                updateCMD.SetAttributeValue("new_status", new OptionSetValue((int)CMAMembershipStatus.Discontinued));
                                updateCMD.SetAttributeValue("new_discontinuedreason", discontinuedReasonRef);
                                updateCMD.SetAttributeValue("new_discontinueddate", DateTime.Today);
                                updateToDiscontinued = true;
                            }
                            //If different category then change of category
                            else if (productRef?.Id != membershipProductCategoryRef.Id && productRef != null)
                                updateCMD.SetAttributeValue("new_categoryproductid", productRef);
                            if (updateToDiscontinued)
                            {
                                ts.Trace($"Checking for other Active");
                                //we discontinued the CMD  
                                foreach (var cmd in cmds)
                                {
                                    // check to see if we have another active membership in the orginal list 
                                    OptionSetValue otherStatus = cmd.GetAttributeValueEx<OptionSetValue>("m.new_status", ts, "cmd.");
                                    Guid otherId = cmd.GetAttributeValueEx<Guid>("m.new_cmamembershipdetailid", ts, "cmd.");
                                    string otherYear = cmd.GetAttributeValueEx<string>("m.new_membershipyear", ts, "cmd.");
                                    if (otherId != membershipDetailRef.Id && otherYear == year && otherStatus?.Value == (int)CMAMembershipStatus.Active)
                                    {
                                        //update the contact's primary membership 
                                        Entity updateContact = new Entity("contact", contactRef.Id);
                                        updateContact.SetAttributeValue("new_cmamembershipdetailid", new EntityReference("new_cmamembershipdetail", otherId));
                                        ts.Trace($"Updating Contact's primary membership");
                                        service.Update(updateContact);
                                        //only do this to first one we find
                                        break;
                                    }
                                }
                            }
                        }

                        //update application
                        CrmHelpers.TraceAttributeCollection(updateCMD, ts);
                        service.Update(updateCMD);
                    }

                }
            }
            else
                errorMessage += "No Membership record found";
            ts.Trace($"Result: '{errorMessage}'");
            Result.Set(context, errorMessage);


        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="amount"></param>
        /// <param name="transactionID"></param>
        /// <param name="ts"></param>
        /// <returns>Returns a transaction id or an Error message begining with the text "Error" </returns>
        public static string CMAAPIRefundPayment(IOrganizationService service, decimal amount, string transactionID, ITracingService ts = null)
        {
            string result = "";

            //get the URL and key
            string apiURL = CmaHelpers.GetConfigLookupValue(service, "Azure/BamboraRefundAPIURL");
            string apiKey = CmaHelpers.GetConfigLookupValue(service, "Azure/BamboraRefundAPIKey");
            string url = apiURL + "singlerefund";
            if (ts != null)
                ts.Trace($"CMAAPIRefundPayment URL: '{url}'");

            string jsonData = $@"{{
                                    ""amount"": ""{amount}"",
                                    ""transactionid"": ""{transactionID}""
                                }}";

            //create client and send request
            var client = new HttpClient();
            StringContent dataContent = string.IsNullOrEmpty(jsonData) ? null : new StringContent(jsonData, Encoding.UTF8, "application/json");
            dataContent.Headers.ContentType.CharSet = "";
            var request = new HttpRequestMessage(new HttpMethod("POST"), url) { Content = dataContent };
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

            var response = client.SendAsync(request).Result;
            result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                if (ts != null)
                    ts.Trace("Sucess");
                try
                {
                    //deserialize result and get the transaction id
                    var successResult = response.Content.ReadAsStringAsync().Result;
                    var successObject = DeserialzeJSON<CMAAPIRefundResult>(successResult);
                    result = successObject.id;

                }
                catch { result = "?"; }
            }
            else
            {
                string errorMessage = "";
                try
                {
                    //deserialize result and get the error message
                    var errorResult = response.Content.ReadAsStringAsync().Result;
                    var errorObject = DeserialzeJSON<CMAAPIError>(errorResult);
                    errorMessage = errorObject == null ? errorMessage : errorObject.message;

                }
                catch { errorMessage = ""; }
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = response.ReasonPhrase;
                result = $"Error Processing credit card refund: {errorMessage}";
            }
            if (ts != null)
                ts.Trace($"CMAAPIRefundPayment Result: '{result}'");
            return result;

        }
        /// <summary>
        /// General purpose deserialize for JSON results
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T DeserialzeJSON<T>(string json)
        {
            T result = default(T);

            MemoryStream deSerializememoryStream = new MemoryStream();
            //initialize DataContractJsonSerializer object and pass Error class type to it
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            //user stream writer to write JSON string data to memory stream
            StreamWriter writer = new StreamWriter(deSerializememoryStream);
            writer.Write(json);
            writer.Flush();
            deSerializememoryStream.Position = 0;
            //get the Desrialized data in object 
            result = (T)serializer.ReadObject(deSerializememoryStream);
            return result;

        }
        public static string GetErrorMessage(IOrganizationService service, Guid userId, string errorCode)
        {
            string result = null;
            string fetchXML = $@"<fetch>
                                 <entity name='new_codelookup' >
                                    <attribute name='new_englishmessage' />
                                    <attribute name='new_frenchmessage' />
                                    <order attribute = 'new_code' descending = 'false' />
                                    <filter>
                                        <condition attribute='new_code' operator='eq' value='{errorCode}' />
                                    </filter>
                                 </entity>
                                 </fetch>";

            var lookupResult = service.RetrieveMultiple(new FetchExpression(fetchXML));
            if (lookupResult.Entities.Count > 0)
            {
                Entity codelookup = lookupResult.Entities[0];
                switch (getUserLanguage(service, userId))
                {
                    case "FR":
                        result = codelookup.GetAttributeValue<string>("new_frenchmessage");
                        break;
                    default:
                        result = codelookup.GetAttributeValue<string>("new_englishmessage");
                        break;
                }
            }
            return result;
        }
        public static string getUserLanguage(IOrganizationService service, Guid userId)
        {
            var code = "EN";
            const int UI_LANGUAGE_FRENCH = 1036;
            string fetchXML = $@"<fetch>
                                 <entity name='usersettings' >
                                    <attribute name='uilanguageid' />
                                    <filter>
                                        <condition attribute='systemuserid' operator='eq' value='{userId}' />
                                    </filter>
                                 </entity>
                                 </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXML));
            if (result.Entities.Count != 0)
            {
                Entity e = result.Entities[0];
                if (e.Contains("uilanguageid"))
                    code = e.GetAttributeValue<int>("uilanguageid") == UI_LANGUAGE_FRENCH ? "FR" : "EN";
            }
            return code;
        }

    }
}
      
        
