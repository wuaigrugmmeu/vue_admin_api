using System.Threading.Tasks;
using UserPermissionSystem.DTOs;
using UserPermissionSystem.Models;

namespace UserPermissionSystem.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, LoginResponseDto? Data)> LoginAsync(LoginRequestDto request);
        Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto request);
        Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword);
        Task<UserDto> GetUserInfoAsync(int userId);
        string GenerateJwtToken(User user);
    }
}