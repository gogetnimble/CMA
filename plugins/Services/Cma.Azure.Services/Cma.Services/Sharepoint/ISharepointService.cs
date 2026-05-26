using Cma.Services.Sharepoint.Contracts;

namespace Cma.Services.Sharepoint;

public interface ISharepointService
{
    Task<CreateFileResponse> CreateFile(CreateFileRequest request);
    Task DeleteItem(DeleteItemRequest request);
    Task<GetFileResponse> GetFile(GetFileRequest request);
    Task<ListItemsResponse> ListItems(ListItemsRequest request);
    Task<CreateFolderResponse> CreateFolder(CreateFolderRequest request);
    Task<GetItemResponse> GetItem(GetItemRequest request);

    Task RenameFileAsync(RenameFileRequest request);
}