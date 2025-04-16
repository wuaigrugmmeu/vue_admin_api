using System.Collections.Generic;

namespace UserPermissionSystem.DTOs
{
    public enum PermissionType
    {
        Menu = 0,
        Button = 1,
        Api = 2,
        Element = 3
    }

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