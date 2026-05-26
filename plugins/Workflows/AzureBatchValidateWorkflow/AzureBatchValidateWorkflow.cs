using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Messages;
using System.Net.Http;

namespace AzureBatchValidateWorkflow
{
    public class AzureBatchValidateWorkflow : CodeActivity
    {

       
        #region Input/Output Arguments        
        [Input("Min Transaction Numbers")]
        [RequiredArgument]
        public InArgument<int> MinTxnNum { get; set; }

        [Input("Max Transaction Number")]
        [RequiredArgument]
        public InArgument<int> MaxTxnNum { get; set; }

        #endregion


        protected override void Execute(CodeActivityContext context)
        {
            // Obtain the organization service reference.
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService ts = context.GetExtension<ITracingService>();

            EntityReference batchRef = new EntityReference(workflowContext.PrimaryEntityName, workflowContext.PrimaryEntityId);
            ts.Trace($"Batch: {batchRef.Id}");

            string countFetchXml = $@"<fetch aggregate='true' >
                                <entity name='new_transaction' >
                                    <attribute name='new_transactionid' alias='Count' aggregate='count' />
                                <filter>
                                  <condition attribute='new_batchid' operator='eq' value='{batchRef.Id}' />
                                </filter>
                              </entity>
                            </fetch>";
            var countResult = GetFirstFromExecuteFetch(service, countFetchXml);
            ts.Trace($"countResult : {countResult }");
            if (countResult != null)
            {
                int? transCount = countResult.GetAttributeValueEx<int?>("Count");
                if (transCount != null && transCount > 0)
                {
                    //string baseURL = @"https://dev-func-cmaamc-d365.azurewebsites.net/api/ValidateBatch?code=XyURvqWPeDCK/nC3KXdeJxV9VcHzaZaguLhnerjXPreTGOGMuoURTg==";
                    string baseURL = GetConfigLookupValue(service, "Azure/ValidateFunctionBaseURL");
                    ts.Trace($"Base URL: {baseURL}");

                    string temp = GetConfigLookupValue(service, "BatchProcessing/ExecuteMultipleMaximumBatchSize");
                    int maxExecuteMultiple = string.IsNullOrWhiteSpace(temp) ? 2 : Convert.ToInt32(temp);
                    ts.Trace($"maxExecuteMultiple: {maxExecuteMultiple}");

                    temp = GetConfigLookupValue(service, "BatchProcessing/ValidateTransactionBlockSize");
                    int transactionBlockSize = string.IsNullOrWhiteSpace(temp) ? 1000 : Convert.ToInt32(temp);
                    ts.Trace($"transactionBlockSize: {transactionBlockSize}");

                    //throw an error if block count exceeds execute multiple
                    int blocks = transCount.Value / transactionBlockSize + (transCount % transactionBlockSize > 0 ? 1 : 0);
                    if (blocks > maxExecuteMultiple)
                        throw new InvalidPluginExecutionException($"Transaction block count {blocks} exceeds CRM maximum ExecuteMultiple: {maxExecuteMultiple}.\n Adjust BatchProcessing/ExecuteMultipleMaximumBatchSize or BatchProcessing/ValidateTransactionBlockSize");
                    ts.Trace($"blocks: {blocks}");

                    //update block count                     
                    Entity batchUpdate = new Entity(batchRef.LogicalName) { Id = batchRef.Id };
                    batchUpdate.SetAttributeValue("new_transactionblockcount", blocks);
                    service.Update(batchUpdate);
                    for (int startTxn = 1; startTxn <= transCount; startTxn += transactionBlockSize)
                    {
                        string url = baseURL + $"&batchid={batchRef.Id}";
                        url += $"&mintxn={startTxn}&maxtxn={startTxn + transactionBlockSize - 1}";
                        HttpClient client = new HttpClient();
                        client.PostAsync(url, null);
                        //HttpResponseMessage response = client.PostAsync(url, null).Result;
                    }
                }
            }

        }

        public static string GetConfigLookupValue(IOrganizationService service, string key)
        {
            string result = null;
            string fetchXML = $@"<fetch>
                                 <entity name='new_customconfiguration' >
                                    <attribute name='new_value' />
                                    <filter>
                                        <condition attribute='new_key' operator='eq' value='{key}' />
                                    </filter>
                                 </entity>
                                 </fetch>";

            var lookupResult = service.RetrieveMultiple(new FetchExpression(fetchXML));
            if (lookupResult.Entities.Count > 0)
                result = lookupResult.Entities[0].GetAttributeValue<string>("new_value");


            return result != null ? result.Trim() : result;
        }
        public static Entity GetFirstFromExecuteFetch(IOrganizationService service, string fetchXml)
        {
            Entity result = null;
            var response = (RetrieveMultipleResponse)service.Execute(new RetrieveMultipleRequest
            {
                Query = new FetchExpression(fetchXml)
            });

            if (response.EntityCollection.Entities.Count > 0)
            {
                result = response.EntityCollection.Entities[0];
            }
            return result;
        }
    }
}      
        
