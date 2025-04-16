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
using UserPermissionSystem.Data;
using UserPermissionSystem.DTOs;
using UserPermissionSystem.Models;

namespace UserPermissionSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<(bool Success, string Message, LoginResponseDto? Data)> LoginAsync(LoginRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserName == request.Username);

            if (user == null)
            {
                return (false, "用户名或密码不正确", null);
            }

            if (!user.IsActive)
            {
                return (false, "用户已被禁用", null);
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash))
            {
                return (false, "用户名或密码不正确", null);
            }

            var token = GenerateJwtToken(user);
            var userDto = await GetUserInfoAsync(user.Id);

            return (true, "登录成功", new LoginResponseDto { Token = token, User = userDto });
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "用户不存在");
            }

            if (!VerifyPasswordHash(request.OldPassword, user.PasswordHash))
            {
                return (false, "原密码不正确");
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "密码修改成功");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "用户不存在");
            }

            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "密码重置成功");
        }

        public async Task<UserDto> GetUserInfoAsync(int userId)
        {
            var user = await _context.Users
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
                .ToList();

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                IsActive = user.IsActive,
                Roles = roles,
                Permissions = permissions
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
            // 确保使用干净的SHA256哈希，与初始数据一致
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hash = Convert.ToBase64String(hashedBytes);
                
                // 打印哈希值用于调试（可以在生产环境中移除）
                Console.WriteLine($"输入密码: {password}");
                Console.WriteLine($"生成哈希: {hash}");
                Console.WriteLine($"存储哈希: {storedHash}");
                
                return hash == storedHash;
            }
        }

        private string HashPassword(string password)
        {
            // 确保使用干净的SHA256哈希，与验证逻辑一致
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}