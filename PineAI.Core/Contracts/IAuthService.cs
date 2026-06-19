using PineAI.Core.Features.Account;

namespace PineAI.Core.Contracts;

public interface IAuthService
{
    Task<GetUserLoginResult> Authenticate(GetUserLoginQuery request);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<List<UserDto>> GetNonAdminUsersAsync();
    Task<CreateUserResult> CreateUserAsync(CreateUserCommand command);
    Task<UpdateUserResult> UpdateUserAsync(UpdateUserCommand command);
    Task<DeleteUserResult> DeleteUserAsync(string userId);
}
