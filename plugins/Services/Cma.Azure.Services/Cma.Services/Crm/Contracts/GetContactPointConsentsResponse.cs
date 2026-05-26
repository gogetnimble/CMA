using Cma.Services.Crm.Models;

namespace Cma.Services.Crm.Contracts;

public record GetContactPointConsentsResponse(List<ContactPointConsent> Consents);