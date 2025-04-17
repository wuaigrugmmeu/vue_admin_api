using System;
using UserPermissionSystem.Domain.Events;

namespace UserPermissionSystem.Domain.Events
{
    // 角色相关的领域事件
    public class RoleCreatedDomainEvent : DomainEvent
    {
        public int RoleId { get; }
        public string RoleName { get; }

        public RoleCreatedDomainEvent(int roleId, string roleName)
        {
            RoleId = roleId;
            RoleName = roleName;
        }
    }

    public class RoleUpdatedDomainEvent : DomainEvent
    {
        public int RoleId { get; }
        public string RoleName { get; }

        public RoleUpdatedDomainEvent(int roleId, string roleName)
        {
            RoleId = roleId;
            RoleName = roleName;
        }
    }

    public class RolePermissionAssignedDomainEvent : DomainEvent
    {
        public int RoleId { get; }
        public string PermissionCode { get; }

        public RolePermissionAssignedDomainEvent(int roleId, string permissionCode)
        {
            RoleId = roleId;
            PermissionCode = permissionCode;
        }
    }

    public class RolePermissionRemovedDomainEvent : DomainEvent
    {
        public int RoleId { get; }
        public string PermissionCode { get; }

        public RolePermissionRemovedDomainEvent(int roleId, string permissionCode)
        {
            RoleId = roleId;
            PermissionCode = permissionCode;
        }
    }
}