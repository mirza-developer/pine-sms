using PineSms.Core.Features.Account;

namespace PineSms.Core.Contracts;

public interface IAuthService
{
    Task<GetUserLoginResult> Authenticate(GetUserLoginQuery request);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<List<UserDto>> GetNonAdminUsersAsync();
    Task<(bool success, string message)> CreateUserAsync(CreateUserCommand command);
    Task<(bool success, string message)> UpdateUserAsync(UpdateUserCommand command);
    Task<(bool success, string message)> DeleteUserAsync(string userId);
}
