using Cma.Services.Crm.Attributes;

namespace Cma.Services.Crm.Contracts;

public record GetEntityModelRequest(params ICrmFilter[] Filters);