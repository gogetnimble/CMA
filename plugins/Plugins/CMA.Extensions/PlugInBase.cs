using Microsoft.Xrm.Sdk;
using System;
using System.Runtime.Remoting.Contexts;

namespace CMA.Extensions.Plugins
{
    public abstract class PluginBase : IPlugin
    {
        protected ITracingService TracingService { get; private set; }
        protected IPluginExecutionContext7 Context { get; private set; }
        protected IOrganizationService Service { get; private set; }
        protected Entity TargetEntity { get; private set; }
        protected string MessageName { get; private set; }
        protected ParameterCollection InputParameters { get; private set; }
        protected ParameterCollection OutputParameters { get; private set; }
        protected EntityImageCollection PreEntityImages { get; private set; }
        protected EntityImageCollection PostEntityImages { get; private set; }

        protected PlugInLogger Logger { get; private set; }

        public void Execute(IServiceProvider serviceProvider)
         {
            Context = (IPluginExecutionContext7)serviceProvider.GetService(typeof(IPluginExecutionContext7));
            TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)
            serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            Service = serviceFactory.CreateOrganizationService(Context.UserId);
            
            Logger = new PlugInLogger(Context, TracingService, Service);

            MessageName = Context.MessageName;
            InputParameters = Context.InputParameters;
            OutputParameters = Context.OutputParameters;

            // Pre/Post images
            PreEntityImages = Context.PreEntityImages;
            PostEntityImages = Context.PostEntityImages;

            // Initialize and perform system checks and assignments
            try
            {
                Logger.DumpContext();

                // Check depth - if greater than 1, exit to prevent infinite loop
                if (Context.Depth > 1)
                {
                    Logger.Trace("Depth is greater than 1, exiting plugin to prevent infinite loop");
                    return;
                }

                // Get the target entity
                if (Context.InputParameters.Contains("Target") &&
                    Context.InputParameters["Target"] is Entity)
                {
                    TargetEntity = (Entity)Context.InputParameters["Target"];

                    // Work with the entity
                    Logger.DumpEntity(TargetEntity);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().Name, ex);
            }

            //Handle my plugin execution.
            try
            {
                ExecutePlugin();
                Logger.Persist();
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().Name, ex);
                throw new InvalidPluginExecutionException($"PlugInBase: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates that TargetEntity is not null and matches the expected early-bound entity type.
        /// Uses generic type to avoid hard-coded logical names.
        /// </summary>
        protected bool ValidateTarget<T>() where T : Entity
        {
            if (TargetEntity == null)
            {
                Logger.Trace("TargetEntity is null");
                return false;
            }

            var expectedLogicalName = Activator.CreateInstance<T>().LogicalName;

            if (TargetEntity.LogicalName != expectedLogicalName)
            {
                Logger.Trace($"Wrong entity. Expected: {expectedLogicalName}, Actual: {TargetEntity.LogicalName}");
                return false;
            }

            return true;
        }
        protected abstract void ExecutePlugin();
    }
}