using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Cma.App.ERP.Functions;

public class BlobToSharepoint
{
    private readonly ILogger<BlobToSharepoint> _logger;

    public BlobToSharepoint(ILogger<BlobToSharepoint> logger)
    {
        _logger = logger;
    }

    [Function(nameof(BlobToSharepoint))]
    public async Task Run([BlobTrigger("samples-workitems/{name}", Connection = "")] Stream stream, string name)
    {
        using var blobStreamReader = new StreamReader(stream);
        var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}", name, content);
    }
}