namespace Cma.Services.Token;

public record TokenServiceConfiguration(
    string GlobalSalt,
    int ActivationTokenExpiryInHours = 72,
    int PasswordResetTokenExpiryInHours = 6,
    int ChangeEmailTokenExpiryInHours = 72);