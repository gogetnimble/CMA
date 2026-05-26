using System;
using Microsoft.Xrm.Sdk;

using CMA.Extensions.Model;
using CMA.Extensions;
/// <summary>
/// [Matches old status and isCmaPrefered with current values, if matches then just update. Otherwise reset old address and set new address as preferred address.]
/// </summary>
/// <remarks>
/// Registration Details:
/// - Entity : [CustomerAddress]
/// - Stage: [Pre-operation]
/// - Execution Mode: [Synchronous]
///
/// Business Logic:
/// 1. Matches old status and isCmaPrefered with current values, if matches then just update. Otherwise reset old address and set new address as preferred address.
///
/// Prerequisites:
/// - .NET 4.6.2
/// - CMA.Extensions library 
/// - ILRepack 
/// </remarks>
namespace CMA.Extensions.Plugins.Address
{
    public class AddressOnPreCreateSetAddressPlugin : PluginBase
    {
        #region Plugin Execution
        protected override void ExecutePlugin()
        {
            var logger = new PlugInLogger(Context, TracingService, Service);

            if (!ValidateTarget<CustomerAddress>())
            {
                logger.Trace("Prerequisites not met, exiting plugin");
                return;
            }

            try
            {
                logger.Trace($"=== {GetType().Name} START ===");
                logger.DumpContext();

                var address = TargetEntity.ToEntity<CustomerAddress>();
                var service = new CustomerAddressService(Service, logger);
                service.HandleCreate(address);
                logger.Trace($"=== {GetType().Name} COMPLETE ===");
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error("An unexpected error occurred while processing the CustomerAddress.", ex);

                throw new InvalidPluginExecutionException(
                $"An error occurred while processing the {TargetEntity.LogicalName}. " +
                $"Please contact your system administrator.", ex);
            }
            finally
            {
                logger.Persist();
            }
        }
        #endregion
    }
}