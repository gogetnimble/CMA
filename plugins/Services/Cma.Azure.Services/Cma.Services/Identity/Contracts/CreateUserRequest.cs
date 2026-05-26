namespace Cma.Services.Identity.Contracts;

public record CreateUserRequest(string Username, string Password, string CmahId)
{
    public string Password { get; set; } = Password;
}