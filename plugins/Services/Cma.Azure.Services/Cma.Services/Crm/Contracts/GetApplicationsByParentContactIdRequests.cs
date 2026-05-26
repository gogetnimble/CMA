namespace Cma.Services.Crm.Contracts;

public record GetApplicationsByParentContactIdRequests(Guid? ParentContactId, string StartYear, string EndYear);