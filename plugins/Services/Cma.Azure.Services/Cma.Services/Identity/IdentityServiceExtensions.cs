using System.Text;
using Cma.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cma.Services.Identity;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityService(this IServiceCollection services, IConfiguration configuration)
    {
        var wso2BaseAddress = configuration.GetValueIfExists<string>("Wso2:BaseAddress");
        var wso2AdminUser = configuration.GetValueIfExists<string>("Wso2:AdminUser");
        var wso2AdminPassword = configuration.GetValueIfExists<string>("Wso2:AdminPassword");

        services.AddHttpClient(nameof(IdentityService), httpClient =>
        {
            httpClient.BaseAddress = new(wso2BaseAddress);
            httpClient.DefaultRequestHeaders.Authorization = new("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{wso2AdminUser}:{wso2AdminPassword}")));
        });

        var identityConfiguration = new IdentityConfiguration(wso2BaseAddress, wso2AdminUser, wso2AdminPassword);
       
        services.AddLogging();
        services.AddSingleton(identityConfiguration);
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}