using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Messages;
using System.Net.Http;

namespace AzureBatchPostWorkflow
{  
    /// </summary>
    public class AzureBatchPostWorkflow : CodeActivity
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

            EntityReference batchRef = new EntityReference( workflowContext.PrimaryEntityName, workflowContext.PrimaryEntityId);
            string countFetchXml = $@"<fetch aggregate='true' >
                                <entity name='new_transaction' >
                                    <attribute name='new_transactionid' alias='Count' aggregate='count' />
                                <filter>
                                  <condition attribute='new_batchid' operator='eq' value='{batchRef.Id}' />
                                </filter>
                              </entity>
                            </fetch>";
            var countResult = GetFirstFromExecuteFetch(service, countFetchXml);
            if (countResult != null)
            {
                                                                                         
                Entity batch = service.Retrieve("new_batch", batchRef.Id, new ColumnSet("new_onlineapplicationid") );
                EntityReference application = batch.GetAttributeValueEx<EntityReference>("new_onlineapplicationid", ts, "batch.");
                int ? transCount = countResult.GetAttributeValueEx<int?>("Count");
                if (transCount != null && transCount > 0)
                {
                    //string baseURL = @"https://dev-func-cmaamc-d365.azurewebsites.net/api/ValidateBatch?code=XyURvqWPeDCK/nC3KXdeJxV9VcHzaZaguLhnerjXPreTGOGMuoURTg==";
                    string baseURL = GetConfigLookupValue(service, "Azure/PostFunctionBaseURL");
                    
                    string temp = GetConfigLookupValue(service, "BatchProcessing/PostTransactionBlockSize");
                    int transactionBlockSize = string.IsNullOrWhiteSpace(temp) ? 1000 : Convert.ToInt32(temp);                    
                    int blocks = transCount.Value / transactionBlockSize + (transCount % transactionBlockSize > 0 ? 1 : 0);

                    //update block count                     
                    Entity batchUpdate = new Entity(batchRef.LogicalName) { Id = batchRef.Id };
                    batchUpdate.SetAttributeValue("new_transactionblockcount", blocks);
                    service.Update(batchUpdate);
                    //call Post Function
                    string url = baseURL + $"&batchid={batchRef.Id}";
                    url += $"&mintxn={1}&maxtxn={20000000}&blocks={blocks}&blocksize={transactionBlockSize}";
                    if(application != null )
                        url += $"&applicationid={application.Id}";
                    ts.Trace($"URL: {url}");
                    HttpClient client = new HttpClient();
                    client.PostAsync(url, null);

                }
            }



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
    }
}
      
        
