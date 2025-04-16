using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UserPermissionSystem.DTOs
{
    public class MenuDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string ComponentPath { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public int Order { get; set; }
        public string? PermissionCode { get; set; }
        public bool IsVisible { get; set; }
        public List<MenuDto> Children { get; set; } = new List<MenuDto>();
    }

    public class CreateMenuDto
    {
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
    }

    public class UpdateMenuDto
    {
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

        public int Order { get; set; }

        [StringLength(100)]
        public string? PermissionCode { get; set; }

        public bool IsVisible { get; set; }
    }
}