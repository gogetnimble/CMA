using Cma.Application.Feature.Aventri;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Cma.App.Http.Functions.Aventri;

public class AventriWebhookFunction
{
    private readonly ILogger<AventriWebhookFunction> _logger;

    public AventriWebhookFunction(ILogger<AventriWebhookFunction> logger)
    {
        _logger = logger;
    }

    [Function("AventriWebhookFunction")]
    [QueueOutput("aventri-inbound", Connection = "AzureWebJobsStorage")]
    public Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, out string queueMessage)
    {
        _logger.LogInformation("Webhook received");

        // Read body synchronously to avoid async method with 'out' parameter
        string body = req.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;


        if (string.IsNullOrWhiteSpace(body))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            bad.WriteStringAsync("Payload is empty").GetAwaiter().GetResult();
            queueMessage = string.Empty; // nothing goes to queue
            return Task.FromResult<HttpResponseData>(bad);
        }


        _logger.LogInformation("request parameter {body}", body);


        // Send the raw body to the queue
        queueMessage = body;

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.WriteStringAsync("Webhook processed successfully").GetAwaiter().GetResult();
        return Task.FromResult<HttpResponseData>(response);
    }
}
