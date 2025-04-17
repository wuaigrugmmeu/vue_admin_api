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
using UserPermissionSystem.Domain.Aggregates.UserAggregate;
using UserPermissionSystem.Domain.Interfaces;
using UserPermissionSystem.Domain.Services;
using UserPermissionSystem.Infrastructure.Authentication;
using UserPermissionSystem.Infrastructure.Persistence;

namespace UserPermissionSystem.Infrastructure.Services
{
    public class AuthDomainService : IAuthDomainService
    {
        private readonly IUserRepository _userRepository;
        private readonly AppDbContext _dbContext;
        private readonly JwtSettings _jwtSettings;

        public AuthDomainService(
            IUserRepository userRepository,
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

            // 使用专用的用户仓储接口查询
            var user = await _userRepository.FindByUserNameAsync(userName);
            
            if (user == null || !user.IsActive)
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
            // 使用专用仓储获取用户权限
            var permissions = await _userRepository.GetUserPermissionsAsync(userId);
            return permissions.ToArray();
        }

        public async Task<bool> ValidateUserPasswordAsync(int userId, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;
            
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;
                
            return user.VerifyPassword(password);
        }
        
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return false;
                
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;
            
            try
            {
                user.ChangePassword(currentPassword, newPassword);
                await _userRepository.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return false;
                
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;
                
            // 使用User类的ResetPassword方法，它会内部处理UpdatedAt属性
            user.ResetPassword(newPassword);
            
            await _userRepository.UnitOfWork.SaveChangesAsync();
            return true;
        }
    }
}