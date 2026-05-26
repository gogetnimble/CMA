using Microsoft.Graph.Models;

namespace Cma.Services.Sharepoint.Contracts;

public record GetItemResponse(DriveItem? DriveItem);