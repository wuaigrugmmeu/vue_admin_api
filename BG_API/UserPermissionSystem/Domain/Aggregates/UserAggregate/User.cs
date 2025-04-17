using System;
using System.Collections.Generic;
using System.Linq;
using UserPermissionSystem.Domain.AggregateModels;
using UserPermissionSystem.Domain.Events;
using UserPermissionSystem.Domain.Exceptions;
using UserPermissionSystem.Domain.ValueObjects;
using UserPermissionSystem.Domain.Entities;

namespace UserPermissionSystem.Domain.Aggregates.UserAggregate
{
    public class User : Entity, IAggregateRoot
    {
        private Password _password;

        public string UserName { get; private set; }
        public string PasswordHash 
        { 
            get => GetPasswordHash();
            private set => SetPasswordHash(value);
        }
        public string Email { get; private set; }
        public string DisplayName { get; private set; }
        public string PhoneNumber { get; private set; }
        public bool IsActive { get; private set; }

        private readonly List<UserRole> _userRoles = new List<UserRole>();
        public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

        // 私有构造函数，防止直接创建实例
        private User() { }

        // 工厂方法，创建新用户
        public static User Create(string userName, string password, string email, string displayName = null, string phoneNumber = null)
        {
            ValidateUserName(userName);
            ValidateEmail(email);

            var user = new User
            {
                UserName = userName,
                Email = email,
                DisplayName = displayName ?? userName,
                PhoneNumber = phoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user._password = Password.Create(password);
            
            user.AddDomainEvent(new UserCreatedDomainEvent(user.Id, user.UserName));
            
            return user;
        }

        // 用于从存储加载的工厂方法
        public static User Load(int id, string userName, string passwordHash, string email, bool isActive, 
            DateTime createdAt, DateTime? updatedAt, string displayName = null, string phoneNumber = null)
        {
            return new User
            {
                Id = id,
                UserName = userName,
                _password = Password.FromHash(passwordHash),
                Email = email,
                DisplayName = displayName ?? userName,
                PhoneNumber = phoneNumber,
                IsActive = isActive,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
        }

        // 更新用户信息
        public void UpdateInfo(string email, string displayName = null, string phoneNumber = null)
        {
            ValidateEmail(email);
            Email = email;
            if (displayName != null) DisplayName = displayName;
            if (phoneNumber != null) PhoneNumber = phoneNumber;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new UserUpdatedDomainEvent(Id, UserName));
        }

        // 修改密码
        public void ChangePassword(string currentPassword, string newPassword)
        {
            if (!_password.VerifyPassword(currentPassword))
                throw new UserDomainException("当前密码不正确");

            _password = Password.Create(newPassword);
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new UserPasswordChangedDomainEvent(Id));
        }

        // 重置密码
        public void ResetPassword(string newPassword)
        {
            _password = Password.Create(newPassword);
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new UserPasswordResetDomainEvent(Id));
        }

        // 公共设置密码方法，供服务层调用
        public void SetPassword(string newPassword)
        {
            _password = Password.Create(newPassword);
            UpdatedAt = DateTime.UtcNow;
        }

        // 激活用户
        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new UserStatusChangedDomainEvent(Id, IsActive));
        }

        // 禁用用户
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new UserStatusChangedDomainEvent(Id, IsActive));
        }

        // 分配角色
        public void AssignRole(Role role)
        {
            if (role == null)
                throw new RoleDomainException("角色不能为空");

            if (_userRoles.Exists(ur => ur.RoleId == role.Id))
                return; // 已经分配了该角色

            _userRoles.Add(new UserRole { UserId = Id, RoleId = role.Id, Role = role });
            
            AddDomainEvent(new UserRoleAssignedDomainEvent(Id, role.Id));
        }

        // 分配角色（通过角色ID）
        public void AssignRoleById(int roleId)
        {
            if (_userRoles.Any(ur => ur.RoleId == roleId))
                return; // 已经分配了该角色

            _userRoles.Add(new UserRole
            {
                UserId = Id,
                RoleId = roleId
            });

            AddDomainEvent(new UserRoleAssignedDomainEvent(Id, roleId));
        }

        // 移除角色
        public void RemoveRole(int roleId)
        {
            var userRole = _userRoles.Find(ur => ur.RoleId == roleId);
            if (userRole != null)
            {
                _userRoles.Remove(userRole);
                
                AddDomainEvent(new UserRoleRemovedDomainEvent(Id, roleId));
            }
        }

        // 验证密码
        public bool VerifyPassword(string password)
        {
            // 添加空值检查防止空引用异常
            if (string.IsNullOrEmpty(password) || _password == null)
                return false;
            
            return _password.VerifyPassword(password);
        }

        // 添加设置器以便EF Core可以设置密码哈希
        private void SetPasswordHash(string passwordHash)
        {
            if (!string.IsNullOrEmpty(passwordHash))
            {
                _password = Password.FromHash(passwordHash);
            }
        }

        // 获取密码哈希的私有方法，供EF Core使用
        private string GetPasswordHash()
        {
            return _password?.PasswordHash;
        }

        // 验证用户名
        private static void ValidateUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new UserDomainException("用户名不能为空");

            if (userName.Length < 3 || userName.Length > 50)
                throw new UserDomainException("用户名长度必须在3-50个字符之间");
        }

        // 验证邮箱
        private static void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new UserDomainException("邮箱不能为空");

            // 简单的邮箱格式验证
            if (!email.Contains("@") || !email.Contains("."))
                throw new UserDomainException("邮箱格式不正确");
        }
    }
}