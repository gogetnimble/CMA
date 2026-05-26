namespace Cma.Services.Sharepoint.Contracts;

public record ListItemsRequest(string DriveId, string ItemId = "root");