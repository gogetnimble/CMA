namespace Cma.Services.Crm.Models;

public record Contact(
    Guid Id,
    string CmahId,
    string PreferredEmailAddress,
    string FirstName,
    string LastName,
    int? Language);