using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UserPermissionSystem.Models
{
    public class Permission
    {
        [Key]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string Module { get; set; } = string.Empty;

        public PermissionType Type { get; set; } = PermissionType.Menu;

        // 导航属性
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();
    }

    public enum PermissionType
    {
        Menu = 0,
        Button = 1,
        Api = 2
    }
}