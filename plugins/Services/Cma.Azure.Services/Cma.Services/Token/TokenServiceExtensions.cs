using Cma.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cma.Services.Token;

public static class TokenServiceExtensions
{
    public static IServiceCollection AddTokenService(this IServiceCollection services, IConfiguration configuration)
    {
        var globalSalt = configuration.GetValueIfExists<string>("Token:GlobalSalt");
        var activationTokenExpiryInHours = configuration.GetValueIfExists<int>("Token:ActivationTokenExpiryInHours");
        var passwordResetTokenExpiryInHours =
            configuration.GetValueIfExists<int>("Token:PasswordResetTokenExpiryInHours");
        var changeEmailTokenExpiryInHours = configuration.GetValueIfExists<int>("Token:ChangeEmailTokenExpiryInHours");


        services.AddLogging();
        services.AddSingleton(new TokenServiceConfiguration(globalSalt, activationTokenExpiryInHours,
            passwordResetTokenExpiryInHours, changeEmailTokenExpiryInHours));
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}