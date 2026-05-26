using Azure.Identity;
using Cma.Common.Exceptions;
using Cma.Common.Extensions;
using Cma.Services.Sharepoint.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Cma.Services.Sharepoint;

public class SharepointService : ISharepointService
{
    public const string DriveRoot = "root";
    private const string DefaultScopes = "https://graph.microsoft.com/.default";
    private readonly GraphServiceClient _graphServiceClient;

    private readonly ILogger<SharepointService> _logger;

    public SharepointService(SharepointServiceConfiguration configuration, ILoggerFactory loggerFactory)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _logger = loggerFactory.CreateLogger<SharepointService>();

        var clientSecretCredential = new ClientSecretCredential(
            configuration.TenantId,
            configuration.ClientId,
            configuration.ClientSecret);

        _graphServiceClient = new(clientSecretCredential, new[] { DefaultScopes });
    }

    /// <summary>
    ///     Creates a file from a byte array
    /// </summary>
    public async Task<CreateFileResponse> CreateFile(CreateFileRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var requestStream = new MemoryStream(request.File);

        var item = await _graphServiceClient
            .Drives[request.DriveId]
            .Items[request.ItemId]
            .Children[request.FileName]
            .Content
            .PutAsync(requestStream);
        
        _logger.LogInformation($"End CreateFile {request.DriveId}/{request.ItemId}");
        
        return new(item);
    }

    /// <summary>
    ///     Deletes an item
    /// </summary>
    public async Task DeleteItem(DeleteItemRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        
        try
        {
            await _graphServiceClient
                .Drives[request.DriveId]
                .Items[request.ItemId]
                .DeleteAsync();

            _logger.LogInformation($"End DeleteItem {request.DriveId}/{request.ItemId}");
        }
        catch (ODataError exception)
        {
            if (exception is { ResponseStatusCode: 404, Message: "The resource could not be found." })
            {
                _logger.LogInformation($"End DeleteItem {request.DriveId}/{request.ItemId} (Item does not exist)");
                return;
            }

            _logger.LogError(exception.Message, exception.StackTrace);
            throw new InternalServerErrorException(ErrorCode.SharePointFailure.ToSnakeCase(), exception.Message, null, exception);
        }
    }

    /// <summary>
    ///     Get a file's contents as a byte array
    /// </summary>
    public async Task<GetFileResponse> GetFile(GetFileRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        Stream? result;
        try
        {
            result = await _graphServiceClient
                .Drives[request.DriveId]
                .Items[request.ItemId]
                .Content
                .GetAsync();
        }
        catch (ODataError exception)
        {
            if (exception is { ResponseStatusCode: 404, Message: "The resource could not be found." })
            {
                _logger.LogInformation($"End GetFile {request.DriveId}/{request.ItemId} (Item does not exist)");
                return new(null);
            }

            _logger.LogError(exception.Message, exception.StackTrace);
            throw new InternalServerErrorException(ErrorCode.SharePointFailure.ToSnakeCase(), exception.Message, null, exception);
        }

        var bytes = Array.Empty<byte>();

        if (result == null)
        {
            return new(bytes);
        }

        using var memoryStream = new MemoryStream();
        await result.CopyToAsync(memoryStream);
        bytes = memoryStream.ToArray();

        _logger.LogInformation($"End GetFile {request.DriveId}/{request.ItemId}");

        return new(bytes);
    }

    /// <summary>
    ///     Lists items in a folder
    /// </summary>
    public async Task<ListItemsResponse> ListItems(ListItemsRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var result = await _graphServiceClient
            .Drives[request.DriveId]
            .Items[request.ItemId]
            .Children
            .GetAsync();

        foreach (var item in result.Value)
        {
            _logger.LogInformation($"Item gotten is  {item?.Name}; \n ID:  {item?.Id} \n Url: {item?.WebUrl} \n Count: {item?.Folder?.ChildCount}");
        }

        _logger.LogInformation($"End ListItems {request.DriveId}/{request.ItemId}");

        return new(result?.Value ?? []);
    }

    /// <summary>
    ///     Creates a folder
    /// </summary>
    public async Task<CreateFolderResponse> CreateFolder(CreateFolderRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var requestBody = new DriveItem
        {
            Name = request.NewFolderName,
            Folder = new(),
            AdditionalData = new Dictionary<string, object>
            {
                {
                    "@microsoft.graph.conflictBehavior", "fail"
                }
            }
        };

        try
        {
            var createFolderResult = await _graphServiceClient
                .Drives[request.DriveId]
                .Items[request.ItemId]
                .Children
                .PostAsync(requestBody);

            _logger.LogInformation($"End CreateFolder {request.DriveId}/{request.ItemId}/{request.NewFolderName}");

            return new(createFolderResult);
        }
        catch (ODataError exception)
        {
            if (exception is { ResponseStatusCode: 409, Message: "Name already exists" })
            {
                _logger.LogInformation($"End CreateFolder {request.DriveId}/{request.ItemId}/{request.NewFolderName} (Item already exists)");
                var getItemResponse = await GetItem(new(request.DriveId, request.ItemId, request.NewFolderName));
                return new(getItemResponse.DriveItem);
            }

            _logger.LogError(exception.Message, exception.StackTrace);
            throw new InternalServerErrorException(ErrorCode.SharePointFailure.ToSnakeCase(), exception.Message, null, exception);
        }
    }

    /// <summary>
    ///     Gets an item
    /// </summary>
    public async Task<GetItemResponse> GetItem(GetItemRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var items = await _graphServiceClient
            .Drives[request.DriveId]
            .Items[request.ItemId]
            .Children
            .GetAsync();

        var item = items?.Value?.FirstOrDefault(x => x.Name == request.ItemName);

        _logger.LogInformation($"End GetItem {request.DriveId}/{request.ItemId}/{request.ItemName}");
        
        _logger.LogInformation($"Item gotten is  {item?.Name}; \n ID:  {item?.Id} \n Url: {item?.WebUrl} \n Count: {item?.Folder?.ChildCount}");

        return new(item);
    }
    
    /// <summary>
    /// Renames a File
    /// </summary>
    /// <param name="request"></param>
    public async Task RenameFileAsync(RenameFileRequest request)
    {
        try
        {
            var update = new Microsoft.Graph.Models.DriveItem()
            {
                Name = request.NewFileName
            };

            await _graphServiceClient.Drives[request.DriveId].Items[request.FileId]
                .Children.PostAsync(update);

            _logger.LogInformation($"File renamed successfully to: {request.NewFileName}");
           
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine($"A required value was null: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Rename operation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message} - {ex.StackTrace}");
        }

    }

}