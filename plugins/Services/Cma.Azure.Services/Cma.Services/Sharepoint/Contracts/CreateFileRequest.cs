namespace Cma.Services.Sharepoint.Contracts;

public record CreateFileRequest(string DriveId, string ItemId, string FileName, byte[] File);