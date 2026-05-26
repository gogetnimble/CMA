namespace Cma.Services.Sharepoint.Contracts;

public record RenameFileRequest(string FileId, string NewFileName, string DriveId);