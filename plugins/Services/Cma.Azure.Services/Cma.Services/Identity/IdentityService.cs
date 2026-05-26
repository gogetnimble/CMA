using System.Net;
using System.Net.Mime;
using System.Text;
using Cma.Common.Exceptions;
using Cma.Common.Extensions;
using Cma.Services.Identity.Contracts;
using Cma.Services.Identity.Models;
using Cma.Services.Identity.Wso2;
using Cma.Services.Identity.Wso2.AccountRecovery;
using Cma.Services.Identity.Wso2.CreateUser;
using Cma.Services.Identity.Wso2.GetUser;
using Humanizer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Wso2Error = Cma.Services.Identity.Wso2.AccountRecovery.Wso2Error;

namespace Cma.Services.Identity;

public class IdentityService : IIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityService> _logger;
    private readonly IdentityConfiguration _configuration;

    public IdentityService(IdentityConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<IdentityService> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(IdentityService));
        _configuration = configuration;
    }

    public async Task<FindUserResponse> FindUserByUsername(FindUserByUsernameRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new FindUserByUsernameRequestValidator().ValidateAndThrowBadRequest(request);
        
        var queryParameters = new Dictionary<string, string?>
        {
            {
                "attributes",
                "userName,urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
            },
            { "filter", $"username eq {request.Username}" }
        };

        return await FindUser(new(queryParameters));
    }

    public async Task<FindUserResponse> FindUserByCmahId(FindUserByCmahIdRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new FindUserByCmahIdRequestValidator().ValidateAndThrowBadRequest(request);
        
        var queryParameters = new Dictionary<string, string?>
        {
            {
                "attributes",
                "userName,emails,urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
            },
            { "filter", $"{Wso2Constants.Schemas.CmahId} eq {request.CmahId}" }
        };

        return await FindUser(new(queryParameters));
    }
    
    public async Task<CreateUserResponse> CreateUser(CreateUserRequest request)
    {
        _logger.LogInformationWithMetadata(request.Clone(x => x.Password = "***"));
        new CreateUserRequestValidator().ValidateAndThrowBadRequest(request);        
        
        // https://is.docs.wso2.com/en/latest/apis/scim2-rest-apis/#/Me%20Endpoint/createUserMe
        const string endpoint = "/scim2/Me";

        var wso2Request = new Wso2UserObject(
            request.Username,
            request.Password,
            new(request.CmahId)
        );
        
        _logger.LogInformationWithMetadata(nameof(wso2Request), wso2Request);
        
        using StringContent body = new(wso2Request.Serialize(), Encoding.UTF8, MediaTypeNames.Application.Json);

        var wso2Response = await _httpClient.PostAsync(endpoint, body);

        var wso2ResponseContent = await wso2Response.Content.ReadAsStringAsync();
        _logger.LogInformationWithMetadata(nameof(wso2ResponseContent), wso2ResponseContent);
        
        if (wso2Response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var wso2ResponseFailure = wso2ResponseContent.Deserialize<Wso2ErrorUnauthorized>();

            throw new InternalServerErrorException(ErrorCode.IdentityFailure.ToSnakeCase(), "Unauthorized",
                wso2ResponseFailure);
        }

        if (wso2Response.StatusCode == HttpStatusCode.BadRequest)
        {
            var wso2ResponseFailure = wso2ResponseContent.Deserialize<Wso2ErrorInvalidInput>();

            throw new InternalServerErrorException(ErrorCode.IdentityFailure.ToSnakeCase(), "Invalid input", wso2ResponseFailure);
        }

        if (wso2Response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var wso2ResponseFailure = wso2ResponseContent.Deserialize<Wso2ErrorInternalServerError>();

            throw new InternalServerErrorException(ErrorCode.IdentityFailure.ToSnakeCase(), "Internal Server Error",
                wso2ResponseFailure);
        }

        // All other errors
        if (!wso2Response.IsSuccessStatusCode)
        {
            throw new InternalServerErrorException(ErrorCode.UnknownError.ToSnakeCase(),
                ErrorCode.IdentityFailure.Humanize());
        }
        
        var wso2ResponseSuccess = wso2ResponseContent.Deserialize<Wso2UserResponseObject>();
        
        // Serialization error
        if (wso2ResponseSuccess == null)
        {
            throw new InternalServerErrorException(
                ErrorCode.SerializationFailure.ToSnakeCase(),
                "Wso2 response could not be deserialized"
            );
        }
        
        _logger.LogInformationWithMetadata(wso2ResponseSuccess);

        // Response
        var response = new CreateUserResponse(new(
            wso2ResponseSuccess.Id,
            wso2ResponseSuccess.User.CmahId,
            wso2ResponseSuccess.User.AccountLock,
            wso2ResponseSuccess.Username,
            wso2ResponseSuccess.User.IsCmaMember
        ));
        _logger.LogInformationWithMetadata(response);
        
        return response;
    }

    public async Task DeleteUser(DeleteUserRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new DeleteUserRequestValidator().ValidateAndThrowBadRequest(request);
        
        // https://is.docs.wso2.com/en/latest/apis/scim2-rest-apis/#/Users%20Endpoint/deleteUser
        var endpoint = $"/scim2/Users/{request.UserId}";
        
        var wso2Response = await _httpClient.DeleteAsync(endpoint);

        var wso2ResponseContent = await wso2Response.Content.ReadAsStringAsync();
        _logger.LogInformationWithMetadata(wso2ResponseContent);

        // If the user is not found, continue
        if (wso2Response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }
        
        if (!wso2Response.IsSuccessStatusCode)
        {
            throw new InternalServerErrorException(ErrorCode.IdentityFailure.ToSnakeCase(),
                "DeleteUser failure", wso2ResponseContent);
        }
    }
    
    public async Task UnlockUser(UnlockUserRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new UnlockUserRequestValidator().ValidateAndThrowBadRequest(request);
        
        // https://is.docs.wso2.com/en/latest/apis/scim2-rest-apis/#/Me%20Endpoint/patchUserMe
        var endpoint = $"/scim2/Users/{request.UserId}";

        var wso2Request = new
        {
            Schemas = new List<string>
            {
                "urn:ietf:params:scim:api:messages:2.0:PatchOp"
            },
            Operations = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["op"] = "add",
                    ["value"] = new Dictionary<string, object>
                    {
                        ["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"] = new
                        {
                            accountLock = false
                        }
                    }
                }
            }
        };

        var serialize = wso2Request.Serialize();
        using StringContent body = new(serialize, Encoding.UTF8, MediaTypeNames.Application.Json);

        var response = await _httpClient.PatchAsync(endpoint, body);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformationWithMetadata(nameof(responseContent), responseContent);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new InternalServerErrorException(ErrorCode.IdentityFailure.ToSnakeCase(), "Could not unlock account",
                responseContent);
        }
    }

    public async Task SetCmahId(SetCmahIdRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new SetCmahIdRequestValidator().ValidateAndThrowBadRequest(request);
        
        // https://is.docs.wso2.com/en/latest/apis/scim2-rest-apis/#/Me%20Endpoint/patchUserMe
        var endpoint = $"/scim2/Users/{request.UserId}";

        var wso2Request = new
        {
            Schemas = new List<string>
            {
                "urn:ietf:params:scim:api:messages:2.0:PatchOp"
            },
            Operations = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["op"] = "add",
                    ["value"] = new Dictionary<string, object>
                    {
                        ["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"] = new
                        {
                            cmahid = request.CmahId
                        }
                    }
                }
            }
        };

        var serialize = wso2Request.Serialize();
        using StringContent body = new(serialize, Encoding.UTF8, MediaTypeNames.Application.Json);

        var response = await _httpClient.PatchAsync(endpoint, body);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformationWithMetadata(nameof(responseContent), responseContent);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new InternalServerErrorException(ErrorCode.IdentityFailure.ToSnakeCase(), "Could not set CmahId",
                responseContent);
        }
    }

    private async Task<FindUserResponse> FindUser(FindUserRequest request)
    {
        // https://is.docs.wso2.com/en/latest/apis/scim2-rest-apis/#/Users%20Endpoint/getUser
        const string endpoint = "scim2/Users";

        var addQueryString = QueryHelpers.AddQueryString(_httpClient.BaseAddress + endpoint, request.QueryParameters);
        var url = new Uri(addQueryString);

        var wso2Response = await _httpClient.GetAsync(url);

        var content = await wso2Response.Content.ReadAsStringAsync();
        
        _logger.LogInformationWithMetadata($"Wso2 GetAdminInfo Response {content}");

        if (!wso2Response.IsSuccessStatusCode)
        {
            throw new InternalServerErrorException(ErrorCode.UnknownError.ToSnakeCase(), content);
        }

        var getUserResponse = content.Deserialize<Wso2GetUserResponse>();

        if (getUserResponse == null)
        {
            throw new InternalServerErrorException(ErrorCode.SerializationFailure.ToSnakeCase(),
                "Wso2GetUserResponse could not be deserialized", content);
        }

        if (getUserResponse.TotalResults != 1 || getUserResponse.Resources.Count == 0)
        {
            var responseInvalidCount = new FindUserResponse(null);
            _logger.LogInformationWithMetadata(responseInvalidCount);
            
            return responseInvalidCount;
        }

        var resources = getUserResponse.Resources.First();

        var user = new User
        (
            resources.Id,
            resources.Wso2User.CmahId,
            resources.Wso2User.AccountLock,
            resources.Username,
            resources.Wso2User.IsCmaMember,
            resources.Emails
        );


        var response = new FindUserResponse(user);
        _logger.LogInformationWithMetadata(response);
        
        return response;
    }

    public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest request)
    {
        _logger.LogInformationWithMetadata(request.Clone(x => x.Password = "***"));
        
        new AuthenticateRequestValidator().ValidateAndThrowBadRequest(request);
        
        var client = new HttpClient();
        client.BaseAddress = new(_configuration.Wso2BaseAddress);
        
        // https://is.docs.wso2.com/en/latest/apis/use-the-authentication-rest-apis/#/Authentication/post_authenticate
        const string endpoint = "api/identity/auth/v1.1/authenticate";
        

        var wso2Request = new Wso2AuthenticationRequest(
            request.Username,
            request.Password);

        using StringContent body = new(wso2Request.Serialize(), Encoding.UTF8, MediaTypeNames.Application.Json);

        var wso2Response = await client.PostAsync(endpoint, body);

        var wso2ResponseContent = await wso2Response.Content.ReadAsStringAsync();
        _logger.LogInformationWithMetadata(nameof(wso2ResponseContent), wso2ResponseContent);

        // Authentication failed
        if (wso2Response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var responseUnauthorized = new AuthenticateResponse(false);
            _logger.LogWarningWithMetadata(responseUnauthorized);

            return responseUnauthorized;
        }

        // All other errors
        if (!wso2Response.IsSuccessStatusCode)
        {
            var wso2ResponseFailure = wso2ResponseContent.Deserialize<Wso2Error>();

            throw new InternalServerErrorException(ErrorCode.UnknownError.ToSnakeCase(),
                wso2ResponseFailure?.Message ?? ErrorCode.IdentityFailure.Humanize(),
                wso2ResponseFailure);
        }
        
        var wso2ResponseSuccess = wso2ResponseContent.Deserialize<Wso2AuthenticationSuccessResponse>();
        // NOTE: This also returns the JWT token, but we don't need that, so just discarding it for now.
        
        // Serialization error
        if (wso2ResponseSuccess == null)
        {
            throw new InternalServerErrorException(
                ErrorCode.SerializationFailure.ToSnakeCase(),
                "Wso2 response could not be deserialized"
            );
        }
        
        _logger.LogInformationWithMetadata(wso2ResponseSuccess);
        
        // Authenticated
        var response = new AuthenticateResponse(true);
        _logger.LogInformationWithMetadata(response);

        return response;
    }
    
    private async Task Wso2ResetPassword(Wso2ResetPasswordRequest wso2Request)
    {
        if (wso2Request == null) throw new BadRequestException(ErrorCode.ArgumentNull.ToSnakeCase(), "Wso2Request is null");
        
        using StringContent body = new(wso2Request.Serialize(), Encoding.UTF8, MediaTypeNames.Application.Json);

        // https://is.docs.wso2.com/en/6.1.0/apis/use-the-account-recovery-rest-apis/#/Password%20Recovery/post_set_password
        const string endpoint = "/api/identity/recovery/v0.9/set-password";
        
        var wso2Response = await _httpClient.PostAsync(endpoint, body);

        var wso2ResponseContent = await wso2Response.Content.ReadAsStringAsync();
        _logger.LogInformationWithMetadata(wso2ResponseContent);
        
        if (!wso2Response.IsSuccessStatusCode)
        {
            throw new InternalServerErrorException(ErrorCode.IdentityFailure.ToSnakeCase(),
                $"Reset password failure", wso2ResponseContent);
        }
    }
    
    private async Task<Wso2RecoveryInitiatingResponse> Wso2RecoveryInitiating(Wso2RecoveryInitiatingRequest wso2Request)
    {
        if (wso2Request == null) throw new BadRequestException(ErrorCode.ArgumentNull.ToSnakeCase(), "Wso2Request is null");
        
        using StringContent body = new(wso2Request.Serialize(), Encoding.UTF8, MediaTypeNames.Application.Json);

        // https://is.docs.wso2.com/en/6.1.0/apis/use-the-account-recovery-rest-apis/#/Password%20Recovery/post_recover_password
        const string endpoint = "/api/identity/recovery/v0.9/recover-password?notify=false";
        
        var wso2Response = await _httpClient.PostAsync(endpoint, body);

        var wso2ResponseContent = await wso2Response.Content.ReadAsStringAsync();
        _logger.LogInformationWithMetadata(wso2ResponseContent);
        
        if (!wso2Response.IsSuccessStatusCode)
        {
            throw new InternalServerErrorException(ErrorCode.IdentityFailure.ToSnakeCase(),
                $"Recover password failure", wso2ResponseContent);
        }
        
        _logger.LogInformationWithMetadata(wso2ResponseContent);

        return new(wso2ResponseContent);
    }
    
    public async Task ChangePassword(ChangePasswordRequest request)
    {
        _logger.LogInformationWithMetadata(request.Clone(x => x.Password = "***"));
        new ChangePasswordRequestValidator().ValidateAndThrowBadRequest(request);
       
        _logger.LogInformation("Recover password");
        var wso2RecoverPasswordRequest = new Wso2RecoveryInitiatingRequest(new(request.Username));
        var wso2RecoverPasswordResponse = await Wso2RecoveryInitiating(wso2RecoverPasswordRequest);

        _logger.LogInformation("Set password");
        var wso2SetPasswordRequest = new Wso2ResetPasswordRequest(wso2RecoverPasswordResponse.Key, request.Password);
        await Wso2ResetPassword(wso2SetPasswordRequest);
    }
}