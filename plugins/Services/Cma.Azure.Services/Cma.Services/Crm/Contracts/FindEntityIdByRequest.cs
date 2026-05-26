namespace Cma.Services.Crm.Contracts;

public record FindEntityIdByRequest(string EntityName, Dictionary<string, object> FindByPropertyKeyValues, bool ReturnAllProperties=false);