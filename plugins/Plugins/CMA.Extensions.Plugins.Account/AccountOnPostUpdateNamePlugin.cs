using CMA.Extensions.Plugins;
using Microsoft.Xrm.Sdk;
using System.Runtime.Remoting.Contexts;
using CMA.Extensions.Model;
using CMA.Extensions;

public class AccountOnPostUpdateNamePlugin : PluginBase
{
    protected override void ExecutePlugin()
    {
        TracingService.Trace($"Processing account: {TargetEntity.Id}");

        Account account = TargetEntity.ToEntity<Account>();

        Entity accountToUpdate = new Entity(Account.EntityLogicalName)
        {
            Id = account.Id
        };
        accountToUpdate[AttributeHelper.AttributeName<Account>(a => a.Name)] = account.Name + " - Processed";
        Service.Update(accountToUpdate);
    }
}