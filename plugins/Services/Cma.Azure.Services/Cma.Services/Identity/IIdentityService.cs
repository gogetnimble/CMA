using Cma.Services.Identity.Contracts;

namespace Cma.Services.Identity;

public interface IIdentityService
{
    Task<FindUserResponse> FindUserByUsername(FindUserByUsernameRequest request);
    Task<CreateUserResponse> CreateUser(CreateUserRequest request);
    Task DeleteUser(DeleteUserRequest request);
    Task<FindUserResponse> FindUserByCmahId(FindUserByCmahIdRequest request);
    Task SetCmahId(SetCmahIdRequest request);
    Task<AuthenticateResponse> Authenticate(AuthenticateRequest request);
    Task UnlockUser(UnlockUserRequest request);
    Task ChangePassword(ChangePasswordRequest request);
}