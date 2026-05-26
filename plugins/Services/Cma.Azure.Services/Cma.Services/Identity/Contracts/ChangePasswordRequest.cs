namespace Cma.Services.Identity.Contracts;

public record ChangePasswordRequest(string Username, string Password)
{
    public string Password { get; set; } = Password;
}