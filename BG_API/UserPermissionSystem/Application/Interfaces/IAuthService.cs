using System.Threading.Tasks;
using UserPermissionSystem.Application.Commands;
using UserPermissionSystem.Domain.Aggregates.UserAggregate;
using UserPermissionSystem.DTOs;

namespace UserPermissionSystem.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<UserInfoDto> GetUserInfoAsync(int userId);
        Task<OperationResult> ChangePasswordAsync(int userId, ChangePasswordDto request);
        Task<OperationResult> ResetPasswordAsync(int userId, string newPassword);
        string GenerateJwtToken(User user);
    }
}