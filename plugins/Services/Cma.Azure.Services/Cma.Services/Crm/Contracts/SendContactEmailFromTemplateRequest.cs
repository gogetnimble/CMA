namespace Cma.Services.Crm.Contracts;

public record SendContactEmailFromTemplateRequest(string TemplateName, Guid ContactId, int ContactLanguage, Dictionary<string, string>? Parameters = null);