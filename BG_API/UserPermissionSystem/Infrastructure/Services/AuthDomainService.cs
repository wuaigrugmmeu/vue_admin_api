using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserPermissionSystem.Domain.Entities;
using UserPermissionSystem.Domain.Interfaces;
using UserPermissionSystem.Domain.Services;
using UserPermissionSystem.Infrastructure.Authentication;
using UserPermissionSystem.Infrastructure.Persistence;

namespace UserPermissionSystem.Infrastructure.Services
{
    public class AuthDomainService : IAuthDomainService
    {
        private readonly IRepository<User> _userRepository;
        private readonly AppDbContext _dbContext;
        private readonly JwtSettings _jwtSettings;

        public AuthDomainService(
            IRepository<User> userRepository,
            AppDbContext dbContext,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        }

        public async Task<User> ValidateUserCredentialsAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;

            // 获取用户
            var user = await _dbContext.Users
                .AsNoTracking()
                .Select(u => User.Load(
                    u.Id,
                    u.UserName,
                    u.PasswordHash,
                    u.Email,
                    u.IsActive,
                    u.CreatedAt,
                    u.UpdatedAt,
                    u.DisplayName,
                    u.PhoneNumber))
                .FirstOrDefaultAsync(u => u.UserName == userName && u.IsActive);

            if (user == null)
                return null;

            // 验证密码
            return user.VerifyPassword(password) ? user : null;
        }

        public async Task<string> GenerateAccessTokenAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // 获取用户权限
            var permissions = await GetUserPermissionsAsync(user.Id);

            // 创建JWT Claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("userId", user.Id.ToString())
            };

            // 添加权限声明
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            // 创建签名密钥
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 创建JWT令牌
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string[]> GetUserPermissionsAsync(int userId)
        {
            // 获取用户的所有权限（通过角色）
            var permissions = await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_dbContext.RolePermissions,
                    ur => ur.RoleId,
                    rp => rp.RoleId,
                    (ur, rp) => rp.PermissionCode)
                .Distinct()
                .ToArrayAsync();

            return permissions;
        }

        public async Task<bool> ValidateUserPasswordAsync(int userId, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;
            
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return false;
                
            return user.VerifyPassword(password);
        }
        
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return false;
                
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return false;
                
            if (!user.VerifyPassword(currentPassword))
                return false;
                
            // 使用User类的SetPassword方法，它会内部处理UpdatedAt属性
            user.SetPassword(newPassword);
            
            await _dbContext.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return false;
                
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                return false;
                
            // 使用User类的ResetPassword方法，它会内部处理UpdatedAt属性
            user.ResetPassword(newPassword);
            
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}