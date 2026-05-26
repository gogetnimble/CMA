namespace Cma.Services.Crm.Contracts;

public record SendContactEmailDirectlyFromTemplateRequest(
    string TemplateName,
    Guid ContactId,
    int ContactLanguage,
    string DirectEmailAddress, 
    Dictionary<string, string>? Parameters = null);