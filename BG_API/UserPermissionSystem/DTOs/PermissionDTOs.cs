using System.Collections.Generic;
using UserPermissionSystem.Models;

namespace UserPermissionSystem.DTOs
{
    public class PermissionDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public PermissionType Type { get; set; }
    }

    public class PermissionGroupDto
    {
        public string Module { get; set; } = string.Empty;
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    }
}