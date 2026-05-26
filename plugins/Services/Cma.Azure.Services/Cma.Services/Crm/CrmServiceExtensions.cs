using Cma.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Cma.Services.Crm;

public static class CrmServiceExtensions
{
    public static IServiceCollection AddCrmService(this IServiceCollection services, IConfiguration configuration)
    {
        var url = configuration.GetValueIfExists<string>("Dynamics:Url");
        var clientId = configuration.GetValueIfExists<string>("Dynamics:ClientId");
        var clientSecret = configuration.GetValueIfExists<string>("Dynamics:ClientSecret");
        var emailQueueIdEnglish = configuration.GetValueIfExists<Guid>("Dynamics:EmailQueueIdEnglish");
        var emailQueueIdFrench = configuration.GetValueIfExists<Guid>("Dynamics:EmailQueueIdFrench");
        var realtimeMarketingPurposeId = configuration.GetValueIfExists<Guid>("Dynamics:RealTimeMarketingPurposeId");

        var crmServiceConfiguration = new CrmServiceConfiguration(
            emailQueueIdEnglish,
            emailQueueIdFrench,
            realtimeMarketingPurposeId
        );

        services.AddLogging();
        services.TryAddSingleton(crmServiceConfiguration);
        services.TryAddSingleton<IOrganizationServiceAsync>(serviceProvider =>
            new ServiceClient(new(url), clientId, clientSecret, false,
                serviceProvider.GetRequiredService<ILogger<CrmService>>())
            {
                MaxRetryCount = 5,
                RetryPauseTime = TimeSpan.FromSeconds(15)
            });
        services.AddScoped<ICrmService, CrmService>();

        return services;
    }
}