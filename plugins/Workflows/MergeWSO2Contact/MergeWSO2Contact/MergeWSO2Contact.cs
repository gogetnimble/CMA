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
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace MergeWSO2Contact
{
    public class MergeWSO2Contact : CodeActivity
    {
        #region Input/Output Arguments        
        [Output("Registered")]
        public OutArgument<bool> Registered { get; set; }
        [Output("Success")]
        public OutArgument<bool> Success { get; set; }
        [Output("Response")]
        public OutArgument<string> Response { get; set; }

        [Input("Merge To Contact")]
        [ReferenceTarget("contact")]
        public InArgument<EntityReference> MergeToContact { get; set; }
        #endregion

        private const string BaseUrlConfigPath="IdentityApi/BaseUrl";
        private const string TokenConfig= "IdentityApi/ClientSecret";
        private const string RetryCountPath= "IdentityApi/RetryCount";
        private const string RetryTimePath= "IdentityApi/RetryTime";
        private const string MergeUserPath="admin-merge";
        private const string CmahIdPathToRemove="{cmahId}/";
        
        
        protected override void Execute(CodeActivityContext context)
        {
            // Obtain the organization service reference.
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService ts = context.GetExtension<ITracingService>();

            Guid contactWithPhysicianContactType = workflowContext.PrimaryEntityId;
            ts.Trace($"Contact With Physician Contact Type: '{ contactWithPhysicianContactType }'");

            EntityReference contactWithContactTypeOthersToMerge = this.MergeToContact != null ? this.MergeToContact.Get(context) : null;
            ts.Trace($"Contact With Contact Type Others To Merge: '{ contactWithContactTypeOthersToMerge?.Name }({contactWithContactTypeOthersToMerge?.Id})'");

            //get cmah id from cma id
            var cmahOldId = getCmahIdForContact(service, contactWithPhysicianContactType);
            var cmahNewId = getCmahIdForContact(service, contactWithContactTypeOthersToMerge.Id);

            var message = $@"{{
                               ""newCmahId"": ""{cmahNewId}"",
                               ""oldCmahId"": ""{cmahOldId}""
                            }}";

			string baseUrl = CmaHelpers.GetConfigLookupValue<string>(service, BaseUrlConfigPath).Trim();
            baseUrl= baseUrl.Replace(CmahIdPathToRemove, "");
            baseUrl += MergeUserPath;
            
			ts.Trace($"URL: {baseUrl}");
            
            string  authToken = GetAuthToken(service);
            
            //no records blocking this one                 
            string errorMessage = String.Empty;

          
             ts.Trace($" HTTPClient  Posting to: {baseUrl}");
             ts.Trace($" {message}");
             
             //initiating api call
             var httpClient = new HttpClient();
             var content = new StringContent(message, Encoding.UTF8, "application/json");
             httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", authToken);

             var result = httpClient.PostAsync(baseUrl, content).Result;
             
             ts.Trace($"responseCode: {result.StatusCode}");
             
             if (result.StatusCode.Equals(System.Net.HttpStatusCode.OK))
             {
                 ts.Trace($"Merge is successful, setting results to true");
                 
                 Registered.Set(context, true);
                 Success.Set(context, true);
                 Response.Set(context, $"200");
                 return;
             }
             
             var errorResult = result.Content.ReadAsStringAsync().Result;
             
             ts.Trace($"Response Content From Azure: {errorResult} ");
             
             if (result.StatusCode.Equals(System.Net.HttpStatusCode.NotFound))
             {
                 var errorObject = DeserializeJson<IdentityError>(errorResult);
                 
                 ts.Trace($"Record not found {errorObject.message}");
                 
                 Registered.Set(context, false);
                 Success.Set(context, false);
                 Response.Set(context, $"404");
             }
             else
             {
                 Success.Set(context, false);
                 var errorObject = DeserializeJson<IdentityError>(errorResult);
                 errorMessage = errorObject.message;
                 Response.Set(context, $"Identity Error: Merge online profile was not successful. Try again later. ({errorMessage})");
             }
        }

        private string getCmahIdForContact(IOrganizationService service, Guid contactId)
        {
            string result = null;
            try
            {
                var cols = new ColumnSet("new_cmah_id");
                var lookupResult = service.Retrieve("contact", contactId, cols);
                if (lookupResult != null)
                {
                    result = lookupResult.GetAttributeValue<string>("new_cmah_id");
                }
            }
            catch { }
            return result;
        }

       

        private static string GetAuthToken(IOrganizationService service)
        {
            var  token = GetConfigLookupValue(service, TokenConfig); 
            return token;
        }
        
        private static string GetConfigLookupValue(IOrganizationService service, string key)
        {
            string result = string.Empty;
            string query = $@"<fetch>
                                 <entity name='new_customconfiguration' >
                                    <attribute name='new_value' />
                                    <filter>
                                        <condition attribute='new_key' operator='eq' value='{key}' />
                                    </filter>
                                 </entity>
                                 </fetch>";

            var lookupResult = service.RetrieveMultiple(new FetchExpression(query));
            if (lookupResult.Entities.Count > 0)
                result = lookupResult.Entities[0].GetAttributeValue<string>("new_value");


            return result;
        }
        
        private static T DeserializeJson<T>(string json) where T : class
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
    }
   
    [DataContract]
    public class IdentityError
    {
        [DataMember]
        public string code { get; set; }
        [DataMember]
        public string message { get; set; }
        [DataMember]
        public object metadata { get; set; }
        [DataMember]
        public string details { get; set; }
    }
    

}
