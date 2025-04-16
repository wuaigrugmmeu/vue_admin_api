using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserPermissionSystem.Data;
using UserPermissionSystem.DTOs;
using UserPermissionSystem.Models;
using UserPermissionSystem.Services;

namespace UserPermissionSystem.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;

        public UsersController(AppDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet]
        [Authorize(Policy = "RequirePermission:user:list")]
        public async Task<ActionResult<PagedResultDto<UserListDto>>> GetUsers([FromQuery] UserPagedRequestDto request)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            // 应用筛选
            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(u =>
                    u.UserName.Contains(request.Search) ||
                    u.Email.Contains(request.Search));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == request.IsActive);
            }

            // 计算总数
            var totalCount = await query.CountAsync();

            // 应用分页
            var users = await query
                .OrderByDescending(u => u.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // 转换为DTO
            var userDtos = users.Select(u => new UserListDto
            {
                Id = u.Id,
                Username = u.UserName,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            }).ToList();

            return Ok(new PagedResultDto<UserListDto>
            {
                Items = userDtos,
                TotalCount = totalCount
            });
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "RequirePermission:user:read")]
        public async Task<ActionResult<UserDetailDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            var roles = user.UserRoles
                .Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description
                })
                .ToList();

            var userDto = new UserDetailDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = user.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                Roles = roles
            };

            return Ok(userDto);
        }

        [HttpPost]
        [Authorize(Policy = "RequirePermission:user:create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            // 检查用户名是否已存在
            if (await _context.Users.AnyAsync(u => u.UserName == createUserDto.Username))
            {
                return BadRequest(new { message = "用户名已存在" });
            }

            // 创建新用户
            var user = new User
            {
                UserName = createUserDto.Username,
                PasswordHash = HashPassword(createUserDto.Password),
                Email = createUserDto.Email ?? string.Empty,
                IsActive = createUserDto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 如果提供了角色，则分配角色
            if (createUserDto.RoleIds != null && createUserDto.RoleIds.Any())
            {
                var validRoleIds = await _context.Roles
                    .Where(r => createUserDto.RoleIds.Contains(r.Id))
                    .Select(r => r.Id)
                    .ToListAsync();

                foreach (var roleId in validRoleIds)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId
                    });
                }

                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, null);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequirePermission:user:update")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            // 更新用户信息
            if (updateUserDto.Email != null)
            {
                user.Email = updateUserDto.Email;
            }

            if (updateUserDto.IsActive.HasValue)
            {
                user.IsActive = updateUserDto.IsActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequirePermission:user:delete")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/roles")]
        [Authorize(Policy = "RequirePermission:user:assignRoles")]
        public async Task<IActionResult> AssignRoles(int id, [FromBody] UserRoleAssignmentDto roleAssignmentDto)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            // 验证角色是否存在
            var validRoleIds = await _context.Roles
                .Where(r => roleAssignmentDto.RoleIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync();

            // 清除现有角色分配
            _context.UserRoles.RemoveRange(user.UserRoles);

            // 添加新的角色分配
            foreach (var roleId in validRoleIds)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                });
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/password")]
        [Authorize(Policy = "RequirePermission:user:resetPassword")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto resetPasswordDto)
        {
            var result = await _authService.ResetPasswordAsync(id, resetPasswordDto.NewPassword);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        private string HashPassword(string password)
        {
            // 使用与AuthService相同的哈希方法
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}