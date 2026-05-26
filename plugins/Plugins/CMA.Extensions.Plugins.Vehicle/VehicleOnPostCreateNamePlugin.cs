using CMA.Extensions;
using CMA.Extensions.Model;
using CMA.Extensions.Plugins;
using Microsoft.Xrm.Sdk;
using System.Runtime.Remoting.Contexts;

public class VehicleOnPostCreateNamePlugin : PluginBase
{
    protected override void ExecutePlugin()
    {
        TracingService.Trace($"Processing Vehicle: {TargetEntity.Id}");

        df_Vehicle vehicle = TargetEntity.ToEntity<df_Vehicle>();

        Entity vehicleToUpdate = new Entity(df_Vehicle.EntityLogicalName)
        {
            Id = vehicle.Id
        };

        vehicleToUpdate[AttributeHelper.AttributeName<df_Vehicle>(a => a.df_Name)] = vehicle.df_Name + " - Processed";
        Service.Update(vehicleToUpdate);
    }
}