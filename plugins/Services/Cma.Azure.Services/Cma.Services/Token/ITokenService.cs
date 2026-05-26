using Cma.Services.Token.Contracts;

namespace Cma.Services.Token;

public interface ITokenService
{
    GenerateNewTokenResponse GenerateNewContactToken(GenerateNewContactTokenRequest request);
    IsTokenExpiredResponse IsActivationTokenExpired(IsTokenExpiredRequest request);
    IsTokenExpiredResponse IsPasswordResetTokenExpired(IsTokenExpiredRequest request);
    ValidateTokenResponse ValidateContactToken(ValidateTokenRequest request);
    IsTokenExpiredResponse IsChangeEmailTokenExpired(IsTokenExpiredRequest request);
}