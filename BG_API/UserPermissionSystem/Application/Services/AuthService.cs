using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserPermissionSystem.Application.Commands;
using UserPermissionSystem.Application.Interfaces;
using UserPermissionSystem.Domain.Aggregates.UserAggregate;
using UserPermissionSystem.Domain.Interfaces;
using UserPermissionSystem.Domain.Services;
using UserPermissionSystem.DTOs;

namespace UserPermissionSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthDomainService _authDomainService;

        public AuthService(IUserRepository userRepository, IAuthDomainService authDomainService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _authDomainService = authDomainService ?? throw new ArgumentNullException(nameof(authDomainService));
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // 调用领域服务验证用户凭据
            var user = await _authDomainService.ValidateUserCredentialsAsync(request.UserName, request.Password);

            if (user == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "用户名或密码错误",
                    UserId = 0,
                    UserName = string.Empty,
                    DisplayName = string.Empty,
                    Token = string.Empty,
                    Permissions = Array.Empty<string>()
                };
            }

            if (!user.IsActive)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "用户账号已被禁用",
                    UserId = 0,
                    UserName = string.Empty,
                    DisplayName = string.Empty,
                    Token = string.Empty,
                    Permissions = Array.Empty<string>()
                };
            }

            // 生成JWT令牌
            var token = await _authDomainService.GenerateAccessTokenAsync(user);
            var permissions = await _authDomainService.GetUserPermissionsAsync(user.Id);

            return new LoginResponseDto
            {
                Success = true,
                Message = "登录成功",
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Token = token,
                Permissions = permissions
            };
        }

        public async Task<OperationResult> ChangePasswordAsync(int userId, ChangePasswordDto request)
        {
            var result = await _authDomainService.ChangePasswordAsync(
                userId, 
                request.OldPassword, 
                request.NewPassword);

            return new OperationResult
            {
                Success = result,
                Message = result ? "密码修改成功" : "密码修改失败，请确认原密码是否正确"
            };
        }

        public async Task<OperationResult> ResetPasswordAsync(int userId, string newPassword)
        {
            var result = await _authDomainService.ResetPasswordAsync(userId, newPassword);

            return new OperationResult
            {
                Success = result,
                Message = result ? "密码重置成功" : "密码重置失败，用户可能不存在"
            };
        }

        public async Task<UserInfoDto> GetUserInfoAsync(int userId)
        {
            // 使用专用仓储
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                throw new KeyNotFoundException($"用户ID {userId} 不存在");
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var permissions = await _authDomainService.GetUserPermissionsAsync(userId);

            return new UserInfoDto
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
                RoleNames = roles,
                Permissions = permissions,
                IsActive = user.IsActive
            };
        }

        public string GenerateJwtToken(User user)
        {
            // 调用领域服务生成Token
            return _authDomainService.GenerateAccessTokenAsync(user).GetAwaiter().GetResult();
        }
    }
}