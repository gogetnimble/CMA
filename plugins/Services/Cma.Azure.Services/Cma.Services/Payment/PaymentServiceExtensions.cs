using System.Net.Mime;
using Cma.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cma.Services.Payment;

public static class PaymentServiceExtensions
{
    public static IServiceCollection AddPaymentService(this IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress = configuration.GetValueIfExists<string>("Bambora:BaseAddress");
        var passcode = configuration.GetValueIfExists<string>("Bambora:Passcode");

        if (passcode.StartsWith("Passcode "))
        {
            passcode = passcode.Replace("Passcode ", "");
        }
        
        services.AddLogging();
        services.AddHttpClient(nameof(PaymentService), httpClient =>
        {
            httpClient.BaseAddress = new(baseAddress);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new(MediaTypeNames.Application.Json));
            httpClient.DefaultRequestHeaders.Authorization = new("Passcode", passcode);
        });

        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}