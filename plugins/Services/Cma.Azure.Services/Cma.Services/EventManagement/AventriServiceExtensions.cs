using Cma.Common.Extensions;
using Cma.Services.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Cma.Services.EventManagement
{
    public static class AventriServiceExtensions
    {
            public static IServiceCollection AddAventriService(this IServiceCollection services, IConfiguration configuration)
            {
            var baseAddress = configuration.GetValueIfExists<string>("Aventri:BaseAddress");
            var passcode = configuration.GetValueIfExists<string>("Aventri:AccessToken");
                        
            services.AddLogging();
            services.AddHttpClient(nameof(PaymentService), httpClient =>
            {
                httpClient.BaseAddress = new(baseAddress);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new(MediaTypeNames.Application.Json));
                //httpClient.DefaultRequestHeaders.Authorization = new("AccessToken", AccessToken);
            });

            services.AddScoped<IAventriService, AventriService>();

            return services;
        }
    }
}
