using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cma.Services.Ftp;

public static class FtpServiceExtensions
{
    public static IServiceCollection AddFtpService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<ISftpClientFactory, SftpClientFactory>();
        services.AddTransient<IFtpService, FtpService>();

        services.AddLogging();
        
        return services;
    }
}