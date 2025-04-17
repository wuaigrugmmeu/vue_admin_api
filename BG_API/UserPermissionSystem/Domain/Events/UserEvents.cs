using System;

namespace UserPermissionSystem.Domain.Events
{
    // 用户相关的领域事件
    public class UserCreatedDomainEvent : DomainEvent
    {
        public int UserId { get; }
        public string UserName { get; }

        public UserCreatedDomainEvent(int userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }
    }

    public class UserUpdatedDomainEvent : DomainEvent
    {
        public int UserId { get; }
        public string UserName { get; }

        public UserUpdatedDomainEvent(int userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }
    }

    public class UserPasswordChangedDomainEvent : DomainEvent
    {
        public int UserId { get; }

        public UserPasswordChangedDomainEvent(int userId)
        {
            UserId = userId;
        }
    }

    public class UserPasswordResetDomainEvent : DomainEvent
    {
        public int UserId { get; }

        public UserPasswordResetDomainEvent(int userId)
        {
            UserId = userId;
        }
    }

    public class UserStatusChangedDomainEvent : DomainEvent
    {
        public int UserId { get; }
        public bool IsActive { get; }

        public UserStatusChangedDomainEvent(int userId, bool isActive)
        {
            UserId = userId;
            IsActive = isActive;
        }
    }

    public class UserRoleAssignedDomainEvent : DomainEvent
    {
        public int UserId { get; }
        public int RoleId { get; }

        public UserRoleAssignedDomainEvent(int userId, int roleId)
        {
            UserId = userId;
            RoleId = roleId;
        }
    }

    public class UserRoleRemovedDomainEvent : DomainEvent
    {
        public int UserId { get; }
        public int RoleId { get; }

        public UserRoleRemovedDomainEvent(int userId, int roleId)
        {
            UserId = userId;
            RoleId = roleId;
        }
    }
}