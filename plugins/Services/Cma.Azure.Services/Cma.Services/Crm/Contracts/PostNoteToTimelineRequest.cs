namespace Cma.Services.Crm.Contracts;

public record PostNoteToTimelineRequest(Guid ContactId, string Subject, string Message);