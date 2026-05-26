using Cma.Services.Crm.Attributes;

namespace Cma.Services.Crm.Contracts;

public record SetEntityModelRequest<T>(T EntityModel, params ICrmFilter[] Filters) where T : new();