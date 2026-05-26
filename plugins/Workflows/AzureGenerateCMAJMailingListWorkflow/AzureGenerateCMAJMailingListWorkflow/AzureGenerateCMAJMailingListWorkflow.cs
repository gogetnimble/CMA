using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Net.Http;

namespace AzureGenerateCMAJMailingListWorkflow
{
	/// </summary>
	public class AzureGenerateCMAJMailingListWorkflow : CodeActivity
	{

		#region Input/Output Arguments

		[Input("Subscription ID")]
		[RequiredArgument]
		public InArgument<string> SubscriptionId { get; set; }

		[Input("Campaign ID")]
		[RequiredArgument]
		public InArgument<string> CampaignId { get; set; }

		[Input("Campaign Name")]
		[RequiredArgument]
		public InArgument<string> CampaignName { get; set; }

        [Input("Old Function")]
        public InArgument<bool> OldFunction { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext context)
		{

			#region WorkflowSetup

			var workflowContext = context.GetExtension<IWorkflowContext>();
			var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
			var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
			var ts = context.GetExtension<ITracingService>();

			#endregion

			var subscriptionId = SubscriptionId.Get<String>(context);
			var campaignId = CampaignId.Get<String>(context);
			var campaignName = CampaignName.Get<String>(context);
			var oldFunction = OldFunction.Get<bool>(context);
			ts.Trace($"subscriptionId : {subscriptionId} \n\r campaignId : {campaignId} \n\r campaignName : {campaignName}");

			string baseURL = GetConfigLookupValue(service, "Azure/GenerateCMAJMailingLabelsListBaseURL").Trim();
			ts.Trace($"URL: {baseURL}");


			ts.Trace("Starting HTTPClient for Posting.");
			var contentRaw = $"{{'SubscriptionId':'{subscriptionId}','CampaignId':'{campaignId}','CampaignName':'{campaignName}'}}";
			var fullURL = RemoveInvalidCharcters($@"{baseURL}&SubscriptionId={subscriptionId}&CampaignId={campaignId}&CampaignName={campaignName}&oldFunction={oldFunction}");
			ts.Trace($"Posting to: {fullURL}");
			var httpClient = new HttpClient();
			httpClient.PostAsync(fullURL, null);

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

        protected static string RemoveInvalidCharcters(string input)
		{
			var output = input.Replace("'", "");
			output = output.Replace("{", "");
			output = output.Replace("}", "");

			return output;

		}
	}


}