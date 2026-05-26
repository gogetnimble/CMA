using CMA.Extensions.Model;
using Microsoft.Xrm.Sdk;
using System;


/// <summary>
/// [Manages CMA preferred addresses by updating related subscription records when a customer address
/// changes its CMA preferred status, handling opt-outs if marked not preferred, and resetting a trigger field after processing.]
/// </summary>
/// <remarks>
/// Registration Details:
/// - Message: [UPdate]
/// - Entity : [CustomerAddress]
/// - Stage: [Post-operation]
/// - Execution Mode: [Asynchronous]
///
/// Business Logic:
/// 1. If not preferred address then
/// -Opt out from all subscriptions
/// -Update opt status as Opted_out
/// 2. Else
/// -Update new indicated address as CMA Preferred Address
/// -Get address type from old address and update all opts associated to this address
/// -Update new_addresstypeforcmapreferredaddress(old_address) field of address to blank value
///
/// Prerequisites:
/// - .NET 4.6.2
/// - CMA.Extensions library
/// - ILRepack
/// </remarks>
namespace CMA.Extensions.Plugins.Address
{
    public class AddressOnPostUpdatePreferredAddressPlugin : PluginBase
    {
        protected override void ExecutePlugin()
        {
            var logger = new PlugInLogger(Context, TracingService, Service);

            if (!ValidateTarget<CustomerAddress>())
            {
                logger.Trace("Prerequisites not met, exiting plugin");
                return;
            }

            if (!PostEntityImages.Contains("PostImage"))
            {
                logger.Warning("Null PostImage - plugin step may be misconfigured!");
                return;
            }

            try
            {
                logger.Trace($"=== {GetType().Name} START ===");
                logger.DumpContext();

                var target = ((Entity)Context.InputParameters["Target"]).ToEntity<CustomerAddress>();
                CustomerAddress postImage = Context.PostEntityImages["PostImage"].ToEntity<CustomerAddress>();
                var serviceLayer = new CustomerAddressService(Service, logger);
                serviceLayer.HandleUpdateCMAPreferred(target, postImage);

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