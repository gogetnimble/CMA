using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Cma.App.Http;

public class AventriQueue
{
    private readonly ILogger<AventriQueue> _logger;

    public AventriQueue(ILogger<AventriQueue> logger)
    {
        _logger = logger;
    }

    [Function(nameof(AventriQueue))]
    public void Run([QueueTrigger("myqueue-items", Connection = "myconnection")] QueueMessage message)
    {
        _logger.LogInformation("C# Queue trigger function processed: {messageText}", message.MessageText);
    }
}