using System;
using System.Security.Cryptography;
using System.Text;
using UserPermissionSystem.Domain.Exceptions;

namespace UserPermissionSystem.Domain.ValueObjects
{
    public class Password
    {
        public string PasswordHash { get; private set; }
        
        // 私有构造函数，防止直接创建实例
        private Password(string passwordHash)
        {
            PasswordHash = passwordHash;
        }
        
        // 从明文密码创建密码值对象
        public static Password Create(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
                throw new UserDomainException("密码不能为空");
                
            if (plainTextPassword.Length < 6)
                throw new UserDomainException("密码长度不能小于6位");
                
            string hash = HashPassword(plainTextPassword);
            return new Password(hash);
        }
        
        // 从哈希值创建密码值对象（用于从数据库加载）
        public static Password FromHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new UserDomainException("密码哈希不能为空");
                
            return new Password(passwordHash);
        }
        
        // 验证密码
        public bool VerifyPassword(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
                return false;
                
            string hashedInput = HashPassword(plainTextPassword);
            return hashedInput == PasswordHash;
        }
        
        // 哈希密码的静态方法
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}