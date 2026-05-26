using Cma.Services.Identity.Models;

namespace Cma.Services.Identity.Contracts;

public record FindUserResponse(User? User)
{
    public bool UserFound => User != null;
}