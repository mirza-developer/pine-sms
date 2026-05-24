using PineSms.Core.Features.Account;

namespace PineSms.Core.Contracts;

public interface IAuthService
{
    Task<GetUserLoginResult> Authenticate(GetUserLoginQuery request);
    Task<List<UserDto>> GetAllUsersAsync();
}
