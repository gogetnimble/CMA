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

namespace OnlineApplicationHasBatch
{
    /// <summary>
    /// Return true if online applictaion has a batch related to it
    /// </summary>
    public class OnlineApplicationHasBatch : CodeActivity
    {

        #region Input/Output Arguments       
        [Output("Online Application Has Batch")]
        public OutArgument<bool> HasBatch { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext context)
        {            
            // Obtain the organization service reference.
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService ts = context.GetExtension<ITracingService>();
            
            //Random random = new Random();
            //int waitTime = random.Next(0, 10) * 10;
            //ts.Trace($"Wait {waitTime}s.");
            //System.Threading.Thread.Sleep(1000 * waitTime); //wait 10 - 100 seconds            

            HasBatch.Set(context, CmaHelpers.OnlineApplicationHasBatch( service, workflowContext.PrimaryEntityId ));

        }
        
    }
}      
        
