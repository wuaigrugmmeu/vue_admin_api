using System.ComponentModel.DataAnnotations.Schema;

namespace UserPermissionSystem.Domain.Entities
{
    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }

        // 导航属性
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
    }
}