using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserPermissionSystem.Application.Commands;
using UserPermissionSystem.Application.Interfaces;
using UserPermissionSystem.Domain.Entities;
using UserPermissionSystem.Domain.Services;
using UserPermissionSystem.DTOs;
using UserPermissionSystem.Infrastructure.Persistence;

namespace UserPermissionSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthDomainService _authDomainService;
        private readonly AppDbContext _dbContext;

        public AuthService(IAuthDomainService authDomainService, AppDbContext dbContext)
        {
            _authDomainService = authDomainService ?? throw new ArgumentNullException(nameof(authDomainService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // 验证用户凭据
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

        public async Task<UserInfoDto> GetUserInfoAsync(int userId)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"用户ID {userId} 不存在");
            }

            var permissions = await _authDomainService.GetUserPermissionsAsync(userId);
            
            return new UserInfoDto
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
                RoleNames = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                Permissions = permissions,
                IsActive = user.IsActive
            };
        }

        public async Task<OperationResult> ChangePasswordAsync(int userId, ChangePasswordDto request)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            
            if (user == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "用户不存在"
                };
            }

            if (!user.VerifyPassword(request.OldPassword))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "当前密码错误"
                };
            }

            user.SetPassword(request.NewPassword);
            await _dbContext.SaveChangesAsync();
            
            return new OperationResult
            {
                Success = true,
                Message = "密码修改成功"
            };
        }

        public async Task<OperationResult> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            
            if (user == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "用户不存在"
                };
            }

            user.SetPassword(newPassword);
            await _dbContext.SaveChangesAsync();
            
            return new OperationResult
            {
                Success = true,
                Message = "密码重置成功"
            };
        }

        public string GenerateJwtToken(User user)
        {
            return _authDomainService.GenerateAccessTokenAsync(user).GetAwaiter().GetResult();
        }
    }
}