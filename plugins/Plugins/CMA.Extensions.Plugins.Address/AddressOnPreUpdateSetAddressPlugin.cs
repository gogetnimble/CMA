using System;
using Microsoft.Xrm.Sdk;

using CMA.Extensions.Model;
using CMA.Extensions;
/// <summary>
/// [Matches old status and isCmaPrefered with current values, if matches then just update. Otherwise reset old address and set new address as preferred address.]
/// </summary>
/// <remarks>
/// Registration Details:
/// - Message: [Update]
/// - Entity : [CustomerAddress]
/// - Stage: [Pre-operation]
/// - Execution Mode: [Synchronous]
///
/// Business Logic:
/// - Matches old status and isCmaPrefered with current values, if matches then just update. Otherwise reset old address and
/// set new address as preferred address.
///
/// Prerequisites:
/// - .NET 4.6.2
/// - CMA.Extensions library
/// - ILRepack
/// </remarks>
namespace CMA.Extensions.Plugins.Address
{
    public class AddressOnPreUpdateSetAddressPlugin : PluginBase
    {
        protected override void ExecutePlugin()
        {
            var logger = new PlugInLogger(Context, TracingService, Service);

            if (!ValidateTarget<CustomerAddress>())
            {
                Logger.Trace("Prerequisites not met, exiting plugin");
                return;
            }

            if (!PreEntityImages.Contains("PreImage"))
            {
                logger.Warning("Null PreImage - plugin step may be misconfigured!");
                return;
            }

            try
            {
                logger.Trace($"=== {GetType().Name} START ===");
                logger.DumpContext();

                var address = TargetEntity.ToEntity<CustomerAddress>();
                CustomerAddress preImage = PreEntityImages["PreImage"].ToEntity<CustomerAddress>();
                var service = new CustomerAddressService(Service, logger);
                service.HandleUpdate(address, preImage);

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
    }
}