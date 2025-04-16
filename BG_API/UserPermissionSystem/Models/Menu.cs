using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserPermissionSystem.Models
{
    public class Menu
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string Path { get; set; } = string.Empty;

        [StringLength(100)]
        public string ComponentPath { get; set; } = string.Empty;

        [StringLength(50)]
        public string Icon { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        public int Order { get; set; } = 0;

        [StringLength(100)]
        public string? PermissionCode { get; set; }

        public bool IsVisible { get; set; } = true;

        // 导航属性
        [ForeignKey("ParentId")]
        public virtual Menu? Parent { get; set; }

        public virtual ICollection<Menu> Children { get; set; } = new List<Menu>();

        [ForeignKey("PermissionCode")]
        public virtual Permission? Permission { get; set; }
    }
}