namespace Cma.Services.Crm.Contracts;

public record UpdateContactPointConsentRequest(Guid ContactPointConsentId, int ConsentStatusKey);