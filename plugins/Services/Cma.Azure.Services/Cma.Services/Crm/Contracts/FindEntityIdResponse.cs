namespace Cma.Services.Crm.Contracts;

public record FindEntityIdResponse(Guid? Id)
{
    public bool EntityFound => Id != null;
}