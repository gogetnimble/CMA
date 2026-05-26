using Azure.Core;
using Azure.Storage.Queues.Models;
using Cma.Application.Feature.Aventri;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System;

namespace Cma.App.Http.Functions.Aventri;

public class AventriQueueFunction
{
    private readonly ILogger<AventriQueueFunction> _logger;
    private readonly IMediator _mediator;

    public AventriQueueFunction(ILogger<AventriQueueFunction> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [Function(nameof(AventriQueueFunction))]
    public void Run([QueueTrigger("aventri-events", Connection = "AzureWebJobsStorage")] QueueMessage message)
    {
        _logger.LogInformation("Aventri Queue function triggered: {messageText}", message.MessageText);

        if (string.IsNullOrEmpty(message.MessageText))
        {
            _logger.LogWarning("Received an empty message. Skipping processing.");
            return;
        }

        //desrialise the message and process it
        var listOfEvents = JsonConvert.DeserializeObject<List<AttendeeEvent>>(message.MessageText);

        //pass to poison queue if deserialization fails
        if (listOfEvents is null || listOfEvents?.Count<=0)
        {
            _logger.LogError("Failed to deserialize message: {messageText}", message.MessageText);
            return;
        }

        //pass to handler for processing
        var response = _mediator.Send(new AttendeeEventRequest(listOfEvents!));

        _logger.LogInformation("Queue trigger functiokoon processed: {messageText}", message.MessageText);
    }
}