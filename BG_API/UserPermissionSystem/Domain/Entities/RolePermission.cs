using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserPermissionSystem.Domain.Aggregates.RoleAggregate;

namespace UserPermissionSystem.Domain.Entities
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        
        [StringLength(100)]
        public string PermissionCode { get; set; } = string.Empty;

        // 导航属性
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        [ForeignKey("PermissionCode")]
        public virtual Permission Permission { get; set; } = null!;
    }
}