namespace Cma.Services.Identity.Contracts;

public record AuthenticateRequest(string Username, string Password)
{
    public string Password { get; set; } = Password;
}