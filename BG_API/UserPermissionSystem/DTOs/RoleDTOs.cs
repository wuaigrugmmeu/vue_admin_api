using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UserPermissionSystem.DTOs
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class RoleDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
        public List<string> PermissionCodes { get; set; } = new List<string>();
    }

    public class CreateRoleDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        public List<string>? PermissionCodes { get; set; }
    }

    public class UpdateRoleDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
    }

    public class RolePermissionAssignmentDto
    {
        [Required]
        public List<string> PermissionCodes { get; set; } = new List<string>();
    }
}