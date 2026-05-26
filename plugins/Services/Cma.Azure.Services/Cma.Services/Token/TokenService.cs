using Cma.Common.Extensions;
using Cma.Services.Token.Contracts;
using Microsoft.Extensions.Logging;

namespace Cma.Services.Token;

public class TokenService : ITokenService
{
    private readonly TokenServiceConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;

    public TokenService(TokenServiceConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public ValidateTokenResponse ValidateContactToken(ValidateTokenRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new ValidateTokenRequestValidator().ValidateAndThrowBadRequest(request);
        
        var generateTokenResponse =
            GenerateContactToken(new(request.ContactId, request.PreferredEmailAddress, request.Timestamp));

        var response = new ValidateTokenResponse(generateTokenResponse.Token == request.Token);
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public GenerateNewTokenResponse GenerateNewContactToken(GenerateNewContactTokenRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new GenerateNewTokenRequestValidator().ValidateAndThrowBadRequest(request);
        
        var timestamp = DateTime.UtcNow.Ticks;
        var generateTokenResponse =
            GenerateContactToken(new(request.ContactId, request.PreferredEmailAddress, timestamp));

        var response = new GenerateNewTokenResponse(generateTokenResponse.Token, timestamp);
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public IsTokenExpiredResponse IsActivationTokenExpired(IsTokenExpiredRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        _logger.LogInformation("ActivationTokenExpiryInHours=" + _configuration.ActivationTokenExpiryInHours );
        var response = IsTokenExpired(request with { ExpiryInHours = _configuration.ActivationTokenExpiryInHours });
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public IsTokenExpiredResponse IsPasswordResetTokenExpired(IsTokenExpiredRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var response = IsTokenExpired(request with { ExpiryInHours = _configuration.PasswordResetTokenExpiryInHours });
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public IsTokenExpiredResponse IsChangeEmailTokenExpired(IsTokenExpiredRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var response = IsTokenExpired(request with { ExpiryInHours = _configuration.ChangeEmailTokenExpiryInHours });
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    private GenerateTokenResponse GenerateContactToken(GenerateContactTokenRequest request)
    {
        var data = $"{request.PreferredEmailAddress}-{request.Timestamp}-{request.ContactId.ToString()}";

        return GenerateToken(new(data));
    }

    private GenerateTokenResponse GenerateToken(GenerateTokenRequest request)
    {
        var token = $"{_configuration.GlobalSalt}-{request.Data}".GenerateSHA512Hash();

        return new(token);
    }

    private static IsTokenExpiredResponse IsTokenExpired(IsTokenExpiredRequest request)
    {
        new IsTokenExpiredRequestValidator().ValidateAndThrowBadRequest(request);
        
        var hours = TimeSpan.FromHours(request.ExpiryInHours);
        var timestamp = new DateTime(request.Timestamp);
        var difference = request.Now - timestamp;

        var isExpired = difference > hours;

        return new(isExpired);
    }
}