using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserPermissionSystem.Application.Commands;
using UserPermissionSystem.Application.Interfaces;
using UserPermissionSystem.Domain.Entities;
using UserPermissionSystem.Domain.Interfaces;
using UserPermissionSystem.DTOs;
using UserPermissionSystem.Infrastructure.Authentication;

namespace UserPermissionSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings)
        {
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var userRepository = _unitOfWork.GetRepository<User>();
            
            // 使用LINQ查询来加载用户及其相关数据
            var user = await userRepository.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserName == request.UserName);

            if (user == null)
            {
                return new LoginResponseDto
                {
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
                    UserId = 0,
                    UserName = string.Empty,
                    DisplayName = string.Empty,
                    Token = string.Empty,
                    Permissions = Array.Empty<string>()
                };
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash))
            {
                return new LoginResponseDto
                {
                    UserId = 0,
                    UserName = string.Empty,
                    DisplayName = string.Empty,
                    Token = string.Empty,
                    Permissions = Array.Empty<string>()
                };
            }

            var token = GenerateJwtToken(user);
            var userInfo = await GetUserInfoAsync(user.Id);

            return new LoginResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Token = token,
                Permissions = userInfo.Permissions.ToArray()
            };
        }

        public async Task<OperationResult> ChangePasswordAsync(int userId, ChangePasswordDto request)
        {
            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "用户不存在"
                };
            }

            // 使用User对象提供的方法修改密码，而不是直接修改属性
            if (!user.VerifyPassword(request.OldPassword))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "原密码不正确"
                };
            }

            user.SetPassword(request.NewPassword);
            
            await userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return new OperationResult
            {
                Success = true,
                Message = "密码修改成功"
            };
        }

        public async Task<OperationResult> ResetPasswordAsync(int userId, string newPassword)
        {
            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "用户不存在"
                };
            }

            // 使用User对象提供的方法重置密码，而不是直接修改属性
            user.ResetPassword(newPassword);
            
            await userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return new OperationResult
            {
                Success = true,
                Message = "密码重置成功"
            };
        }

        public async Task<UserInfoDto> GetUserInfoAsync(int userId)
        {
            var userRepository = _unitOfWork.GetRepository<User>();
            
            var user = await userRepository.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"用户ID {userId} 不存在");
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToArray();

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
            var roleClaims = user.UserRoles
                .Select(ur => new Claim(ClaimTypes.Role, ur.Role.Name))
                .ToList();

            var permissionClaims = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => new Claim("permission", rp.Permission.Code))
                .Distinct()
                .ToList();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            claims.AddRange(roleClaims);
            claims.AddRange(permissionClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(_jwtSettings.ExpirationInMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hash = Convert.ToBase64String(hashedBytes);
                
                return hash == storedHash;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}