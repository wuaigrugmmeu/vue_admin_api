using System.Collections.Generic;
using UserPermissionSystem.Domain.Events;
using UserPermissionSystem.Domain.Exceptions;

namespace UserPermissionSystem.Domain.Entities
{
    public enum PermissionType
    {
        Api,
        Menu,
        Button
    }
    
    public class Permission : Entity
    {
        public string Code { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Module { get; private set; } = string.Empty;
        public PermissionType Type { get; private set; }
        
        private readonly List<RolePermission> _rolePermissions = new List<RolePermission>();
        public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();
        
        // 添加Menus导航属性
        private readonly List<Menu> _menus = new List<Menu>();
        public IReadOnlyCollection<Menu> Menus => _menus.AsReadOnly();

        // 私有构造函数，防止直接创建实例
        private Permission() { }
        
        // 工厂方法，创建新权限
        public static Permission Create(string code, string name, string description, string module, PermissionType type)
        {
            ValidateCode(code);
            ValidateName(name);

            var permission = new Permission
            {
                Code = code,
                Name = name,
                Description = description ?? string.Empty,
                Module = module ?? string.Empty,
                Type = type
            };
            
            permission.AddDomainEvent(new PermissionCreatedDomainEvent(code, name));
            
            return permission;
        }
        
        // 用于从存储加载的工厂方法
        public static Permission Load(string code, string name, string description, string module, PermissionType type)
        {
            return new Permission
            {
                Code = code,
                Name = name,
                Description = description ?? string.Empty,
                Module = module ?? string.Empty,
                Type = type
            };
        }
        
        // 更新权限信息
        public void Update(string name, string description, string module, PermissionType type)
        {
            ValidateName(name);
            
            Name = name;
            Description = description ?? string.Empty;
            Module = module ?? string.Empty;
            Type = type;
            
            AddDomainEvent(new PermissionUpdatedDomainEvent(Code, Name));
        }
        
        // 验证权限代码
        private static void ValidateCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new PermissionDomainException("权限代码不能为空");
                
            if (code.Length > 50)
                throw new PermissionDomainException("权限代码长度不能超过50个字符");
        }
        
        // 验证权限名称
        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new PermissionDomainException("权限名称不能为空");
                
            if (name.Length > 50)
                throw new PermissionDomainException("权限名称长度不能超过50个字符");
        }
    }
    
    // 权限相关的领域事件
    public class PermissionCreatedDomainEvent : DomainEvent
    {
        public string PermissionCode { get; }
        public string PermissionName { get; }
        
        public PermissionCreatedDomainEvent(string permissionCode, string permissionName)
        {
            PermissionCode = permissionCode;
            PermissionName = permissionName;
        }
    }
    
    public class PermissionUpdatedDomainEvent : DomainEvent
    {
        public string PermissionCode { get; }
        public string PermissionName { get; }
        
        public PermissionUpdatedDomainEvent(string permissionCode, string permissionName)
        {
            PermissionCode = permissionCode;
            PermissionName = permissionName;
        }
    }
}