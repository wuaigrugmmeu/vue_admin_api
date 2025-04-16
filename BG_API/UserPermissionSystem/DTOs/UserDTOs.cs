using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UserPermissionSystem.DTOs
{
    public class CreateUserDto
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        public List<int>? RoleIds { get; set; }
    }

    public class UpdateUserDto
    {
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public bool? IsActive { get; set; }
    }

    public class UserRoleAssignmentDto
    {
        [Required]
        public List<int> RoleIds { get; set; } = new List<int>();
    }

    public class UserDetailDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
        public List<RoleDto> Roles { get; set; } = new List<RoleDto>();
    }

    public class UserListDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
    }

    public class PagedRequestDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
    }

    public class UserPagedRequestDto : PagedRequestDto
    {
        public bool? IsActive { get; set; }
    }
}