namespace Cma.Services.Crm.Contracts;

public record CreateContactRequest(
    string PreferredEmailAddress,
    string FirstName,
    string LastName,
    string LanguageCode,
    int CreationReason,
    bool IsSelfIdentifiedPhysician=false);