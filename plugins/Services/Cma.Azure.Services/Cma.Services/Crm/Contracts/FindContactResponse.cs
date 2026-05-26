using Cma.Services.Crm.Models;

namespace Cma.Services.Crm.Contracts;

public record FindContactResponse(Contact? Contact)
{
    public bool ContactFound => Contact != null;
}