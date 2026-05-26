namespace Cma.Services.Crm.Contracts;

public record AutoPostToTimelineRequest(Guid ContactId, string Message);