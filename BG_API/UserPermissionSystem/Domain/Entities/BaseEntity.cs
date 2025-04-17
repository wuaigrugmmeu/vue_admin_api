using System;
using System.ComponentModel.DataAnnotations;
using UserPermissionSystem.Domain.Events;

namespace UserPermissionSystem.Domain.Entities
{
    public abstract class BaseEntity
    {
        public int Id { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime? UpdatedAt { get; protected set; }
        
        // 添加并发控制版本戳
        [Timestamp]
        public byte[] RowVersion { get; set; }

        protected BaseEntity()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}