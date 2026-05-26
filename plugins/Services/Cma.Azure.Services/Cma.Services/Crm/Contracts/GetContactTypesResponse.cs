using Cma.Services.Crm.Models;

namespace Cma.Services.Crm.Contracts;

public record GetContactTypesResponse(List<ContactType> ContactTypes);