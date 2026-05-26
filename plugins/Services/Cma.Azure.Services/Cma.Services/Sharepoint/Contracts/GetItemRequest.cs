namespace Cma.Services.Sharepoint.Contracts;

public record GetItemRequest(string DriveId, string ItemId, string ItemName);