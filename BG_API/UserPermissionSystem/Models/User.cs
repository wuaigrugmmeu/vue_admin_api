using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UserPermissionSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // 导航属性
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}