using Cma.Services.Ftp.Contracts;

namespace Cma.Services.Ftp;

public interface IFtpService
{
    Task Connect(ConnectRequest request);
    
    Task<ListFilesResponse> ListFiles(ListFilesRequest request);
    Task<GetFileResponse> GetFile(GetFileRequest request);

    Task DeleteFile(DeleteFileRequest request);
    bool IsConnected { get; }
    Task UploadFile(UploadFileRequest request);

    Task CreateFolder(CreateFolderRequest request);
    Task DeleteFolder(DeleteFolderRequest request);
    Task Disconnect();
}