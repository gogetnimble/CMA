namespace Cma.Services.Sharepoint.Contracts;

public record CreateFolderRequest(string DriveId, string ItemId, string NewFolderName);