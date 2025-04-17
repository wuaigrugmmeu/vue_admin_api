using System;
using System.Collections.Generic;
using System.Linq;
using UserPermissionSystem.Domain.AggregateModels;
using UserPermissionSystem.Domain.Entities;
using UserPermissionSystem.Domain.Exceptions;

namespace UserPermissionSystem.Domain.Aggregates.RoleAggregate
{
    public class Role : Entity, IAggregateRoot
    {
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;

        private readonly List<UserRole> _userRoles = new List<UserRole>();
        public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

        private readonly List<RolePermission> _rolePermissions = new List<RolePermission>();
        public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

        // 私有构造函数，防止直接创建实例
        private Role() { }

        // 工厂方法，创建新角色
        public static Role Create(string name, string description)
        {
            ValidateName(name);

            var role = new Role
            {
                Name = name,
                Description = description ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            role.AddDomainEvent(new UserPermissionSystem.Domain.Events.RoleCreatedDomainEvent(role.Id, role.Name));

            return role;
        }

        // 用于从存储加载的工厂方法
        public static Role Load(int id, string name, string description, DateTime createdAt, DateTime? updatedAt)
        {
            return new Role
            {
                Id = id,
                Name = name,
                Description = description ?? string.Empty,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
        }

        // 更新角色信息
        public void Update(string name, string description)
        {
            ValidateName(name);

            Name = name;
            Description = description ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new UserPermissionSystem.Domain.Events.RoleUpdatedDomainEvent(Id, Name));
        }

        // 分配权限
        public void AssignPermission(Permission permission)
        {
            if (permission == null)
                throw new PermissionDomainException("权限不能为空");

            if (_rolePermissions.Any(rp => rp.PermissionCode == permission.Code))
                return; // 已经分配了该权限

            _rolePermissions.Add(new RolePermission 
            { 
                RoleId = Id, 
                PermissionCode = permission.Code,
                Permission = permission 
            });

            AddDomainEvent(new UserPermissionSystem.Domain.Events.RolePermissionAssignedDomainEvent(Id, permission.Code));
        }

        // 批量分配权限
        public void AssignPermissions(IEnumerable<Permission> permissions)
        {
            if (permissions == null)
                throw new PermissionDomainException("权限集合不能为空");

            foreach (var permission in permissions)
            {
                AssignPermission(permission);
            }
        }

        // 移除权限
        public void RemovePermission(string permissionCode)
        {
            var rolePermission = _rolePermissions.FirstOrDefault(rp => rp.PermissionCode == permissionCode);
            if (rolePermission != null)
            {
                _rolePermissions.Remove(rolePermission);
                AddDomainEvent(new UserPermissionSystem.Domain.Events.RolePermissionRemovedDomainEvent(Id, permissionCode));
            }
        }

        // 验证角色名称
        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new RoleDomainException("角色名称不能为空");

            if (name.Length > 50)
                throw new RoleDomainException("角色名称长度不能超过50个字符");
        }
    }
}