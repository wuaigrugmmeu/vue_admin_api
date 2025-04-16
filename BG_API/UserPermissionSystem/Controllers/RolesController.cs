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

namespace UserPermissionSystem.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RolesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "RequirePermission:role:list")]
        public async Task<ActionResult<PagedResultDto<RoleDto>>> GetRoles([FromQuery] PagedRequestDto request)
        {
            var query = _context.Roles.AsQueryable();

            // 应用搜索条件
            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(r => r.Name.Contains(request.Search) || r.Description.Contains(request.Search));
            }

            // 计算总数
            var totalCount = await query.CountAsync();

            // 应用分页
            var roles = await query
                .OrderByDescending(r => r.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // 转换为DTO
            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            }).ToList();

            return Ok(new PagedResultDto<RoleDto>
            {
                Items = roleDtos,
                TotalCount = totalCount
            });
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<ActionResult<List<RoleDto>>> GetAllRoles()
        {
            var roles = await _context.Roles.ToListAsync();

            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            }).ToList();

            return Ok(roleDtos);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "RequirePermission:role:read")]
        public async Task<ActionResult<RoleDetailDto>> GetRole(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound(new { message = "角色不存在" });
            }

            var roleDto = new RoleDetailDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = role.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                PermissionCodes = role.RolePermissions.Select(rp => rp.PermissionCode).ToList()
            };

            return Ok(roleDto);
        }

        [HttpPost]
        [Authorize(Policy = "RequirePermission:role:create")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto createRoleDto)
        {
            // 检查角色名是否已存在
            if (await _context.Roles.AnyAsync(r => r.Name == createRoleDto.Name))
            {
                return BadRequest(new { message = "角色名已存在" });
            }

            // 创建新角色
            var role = new Role
            {
                Name = createRoleDto.Name,
                Description = createRoleDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // 如果提供了权限，则分配权限
            if (createRoleDto.PermissionCodes != null && createRoleDto.PermissionCodes.Any())
            {
                var validPermissionCodes = await _context.Permissions
                    .Where(p => createRoleDto.PermissionCodes.Contains(p.Code))
                    .Select(p => p.Code)
                    .ToListAsync();

                foreach (var code in validPermissionCodes)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionCode = code
                    });
                }

                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, null);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequirePermission:role:update")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto updateRoleDto)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound(new { message = "角色不存在" });
            }

            // 检查更新后的角色名是否与其他角色冲突
            if (await _context.Roles.AnyAsync(r => r.Id != id && r.Name == updateRoleDto.Name))
            {
                return BadRequest(new { message = "角色名已存在" });
            }

            // 更新角色信息
            role.Name = updateRoleDto.Name;
            role.Description = updateRoleDto.Description;
            role.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequirePermission:role:delete")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound(new { message = "角色不存在" });
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/permissions")]
        [Authorize(Policy = "RequirePermission:role:assignPermissions")]
        public async Task<IActionResult> AssignPermissions(int id, [FromBody] RolePermissionAssignmentDto permissionAssignmentDto)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound(new { message = "角色不存在" });
            }

            // 验证权限是否存在
            var validPermissionCodes = await _context.Permissions
                .Where(p => permissionAssignmentDto.PermissionCodes.Contains(p.Code))
                .Select(p => p.Code)
                .ToListAsync();

            // 清除现有权限分配
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // 添加新的权限分配
            foreach (var code in validPermissionCodes)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionCode = code
                });
            }

            role.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}