using Cma.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cma.Services.Sharepoint;

public static class SharepointServiceExtensions
{
    public static IServiceCollection AddSharepointService(this IServiceCollection services,
        IConfiguration configuration)
    {
        var tenantId = configuration.GetValueIfExists<string>("TenantId");
        var clientId = configuration.GetValueIfExists<string>("ClientId");
        var clientSecret = configuration.GetValueIfExists<string>("ClientSecret");

        var sharepointServiceConfiguration = new SharepointServiceConfiguration(tenantId, clientId, clientSecret);

        services.AddLogging();
        services.AddSingleton(sharepointServiceConfiguration);
        services.AddScoped<ISharepointService, SharepointService>();

        return services;
    }
}