using Microsoft.Graph.Models;

namespace Cma.Services.Sharepoint.Contracts;

public record ListItemsResponse(List<DriveItem> DriveItems);