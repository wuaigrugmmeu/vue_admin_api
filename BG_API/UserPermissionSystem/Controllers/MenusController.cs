using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserPermissionSystem.Infrastructure.Persistence;
using UserPermissionSystem.DTOs;
using UserPermissionSystem.Domain.Entities;

namespace UserPermissionSystem.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MenusController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MenusController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "RequirePermission:menu:list")]
        public async Task<ActionResult<IEnumerable<MenuDto>>> GetMenus()
        {
            var menus = await _context.Menus.ToListAsync();

            var menuDtos = menus.Select(m => new MenuDto
            {
                Id = m.Id,
                Name = m.Name,
                Path = m.Path,
                ComponentPath = m.ComponentPath,
                Icon = m.Icon,
                ParentId = m.ParentId,
                Order = m.Order,
                PermissionCode = m.PermissionCode,
                IsVisible = m.IsVisible
            }).ToList();

            return Ok(menuDtos);
        }

        [HttpGet("tree")]
        [Authorize(Policy = "RequirePermission:menu:list")]
        public async Task<ActionResult<List<MenuDto>>> GetMenuTree()
        {
            var menus = await _context.Menus
                .OrderBy(m => m.Order)
                .ToListAsync();

            // 将扁平菜单列表转换为树形结构
            var menuDtos = menus.Select(m => new MenuDto
            {
                Id = m.Id,
                Name = m.Name,
                Path = m.Path,
                ComponentPath = m.ComponentPath,
                Icon = m.Icon,
                ParentId = m.ParentId,
                Order = m.Order,
                PermissionCode = m.PermissionCode,
                IsVisible = m.IsVisible
            }).ToList();

            var menuTree = BuildMenuTree(menuDtos);
            return Ok(menuTree);
        }

        [HttpGet("mytree")]
        [Authorize]
        public async Task<ActionResult<List<MenuDto>>> GetMyMenuTree()
        {
            // 获取当前用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "无效的用户身份" });
            }

            // 获取用户所拥有的权限
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            var permissionCodes = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.PermissionCode)
                .Distinct()
                .ToList();

            // 获取用户有权访问的所有菜单
            var visibleMenus = await _context.Menus
                .Where(m => m.IsVisible && (string.IsNullOrEmpty(m.PermissionCode) || permissionCodes.Contains(m.PermissionCode)))
                .OrderBy(m => m.Order)
                .ToListAsync();

            // 转换为DTO
            var menuDtos = visibleMenus.Select(m => new MenuDto
            {
                Id = m.Id,
                Name = m.Name,
                Path = m.Path,
                ComponentPath = m.ComponentPath,
                Icon = m.Icon,
                ParentId = m.ParentId,
                Order = m.Order,
                PermissionCode = m.PermissionCode,
                IsVisible = m.IsVisible
            }).ToList();

            // 构建树形菜单，保留有效的父节点路径
            var menuTree = BuildAccessibleMenuTree(menuDtos);
            return Ok(menuTree);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "RequirePermission:menu:read")]
        public async Task<ActionResult<MenuDto>> GetMenu(int id)
        {
            var menu = await _context.Menus
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menu == null)
            {
                return NotFound(new { message = "菜单不存在" });
            }

            var menuDto = new MenuDto
            {
                Id = menu.Id,
                Name = menu.Name,
                Path = menu.Path,
                ComponentPath = menu.ComponentPath,
                Icon = menu.Icon,
                ParentId = menu.ParentId,
                Order = menu.Order,
                PermissionCode = menu.PermissionCode,
                IsVisible = menu.IsVisible
            };

            return Ok(menuDto);
        }

        [HttpPost]
        [Authorize(Policy = "RequirePermission:menu:create")]
        public async Task<IActionResult> CreateMenu([FromBody] CreateMenuDto createMenuDto)
        {
            // 验证父菜单是否存在
            if (createMenuDto.ParentId.HasValue)
            {
                var parentExists = await _context.Menus.AnyAsync(m => m.Id == createMenuDto.ParentId);
                if (!parentExists)
                {
                    return BadRequest(new { message = "父菜单不存在" });
                }
            }

            // 验证权限码是否存在
            if (!string.IsNullOrEmpty(createMenuDto.PermissionCode))
            {
                var permissionExists = await _context.Permissions.AnyAsync(p => p.Code == createMenuDto.PermissionCode);
                if (!permissionExists)
                {
                    return BadRequest(new { message = "权限码不存在" });
                }
            }

            // 使用Menu实体的工厂方法创建新菜单
            var menu = Menu.Create(
                createMenuDto.Name,
                createMenuDto.Path,
                createMenuDto.ComponentPath,
                createMenuDto.Icon,
                createMenuDto.ParentId,
                createMenuDto.Order,
                createMenuDto.PermissionCode,
                createMenuDto.IsVisible
            );

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMenu), new { id = menu.Id }, null);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequirePermission:menu:update")]
        public async Task<IActionResult> UpdateMenu(int id, [FromBody] UpdateMenuDto updateMenuDto)
        {
            var menu = await _context.Menus.FindAsync(id);

            if (menu == null)
            {
                return NotFound(new { message = "菜单不存在" });
            }

            // 验证父菜单是否存在且不是自身或自身的子菜单（避免循环引用）
            if (updateMenuDto.ParentId.HasValue)
            {
                if (updateMenuDto.ParentId.Value == id)
                {
                    return BadRequest(new { message = "菜单不能将自身设为父菜单" });
                }

                var parentMenus = new HashSet<int?>();
                var currentParentId = updateMenuDto.ParentId;
                
                while (currentParentId.HasValue)
                {
                    if (parentMenus.Contains(currentParentId))
                    {
                        return BadRequest(new { message = "检测到菜单层级循环引用" });
                    }
                    
                    parentMenus.Add(currentParentId);
                    
                    var parentMenu = await _context.Menus.FindAsync(currentParentId.Value);
                    if (parentMenu == null)
                    {
                        return BadRequest(new { message = "父菜单不存在" });
                    }
                    
                    if (currentParentId.Value == id)
                    {
                        return BadRequest(new { message = "不能将自己的子菜单设置为父菜单，这会导致循环引用" });
                    }
                    
                    currentParentId = parentMenu.ParentId;
                }
            }

            // 验证权限码是否存在
            if (!string.IsNullOrEmpty(updateMenuDto.PermissionCode))
            {
                var permissionExists = await _context.Permissions.AnyAsync(p => p.Code == updateMenuDto.PermissionCode);
                if (!permissionExists)
                {
                    return BadRequest(new { message = "权限码不存在" });
                }
            }

            // 使用Menu实体的Update方法更新菜单
            menu.Update(
                updateMenuDto.Name,
                updateMenuDto.Path,
                updateMenuDto.ComponentPath,
                updateMenuDto.Icon,
                updateMenuDto.ParentId,
                updateMenuDto.Order,
                updateMenuDto.PermissionCode,
                updateMenuDto.IsVisible
            );

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequirePermission:menu:delete")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var menu = await _context.Menus.FindAsync(id);

            if (menu == null)
            {
                return NotFound(new { message = "菜单不存在" });
            }

            // 检查是否有子菜单
            var hasChildren = await _context.Menus.AnyAsync(m => m.ParentId == id);
            if (hasChildren)
            {
                return BadRequest(new { message = "请先删除该菜单的所有子菜单" });
            }

            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 构建菜单树的辅助方法
        private List<MenuDto> BuildMenuTree(List<MenuDto> menus, int? parentId = null)
        {
            return menus
                .Where(m => m.ParentId == parentId)
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Path = m.Path,
                    ComponentPath = m.ComponentPath,
                    Icon = m.Icon,
                    ParentId = m.ParentId,
                    Order = m.Order,
                    PermissionCode = m.PermissionCode,
                    IsVisible = m.IsVisible,
                    Children = BuildMenuTree(menus, m.Id)
                })
                .OrderBy(m => m.Order)
                .ToList();
        }

        // 构建可访问的菜单树，确保包含父级菜单路径
        private List<MenuDto> BuildAccessibleMenuTree(List<MenuDto> menus)
        {
            // 找出有访问权限的菜单ID
            var accessibleIds = new HashSet<int>(menus.Select(m => m.Id));

            // 确保显示父节点（即使没有直接权限）
            var completeMenus = menus.ToList();
            foreach (var menu in menus.Where(m => m.ParentId.HasValue))
            {
                EnsureParentPath(completeMenus, menu.ParentId.Value, accessibleIds);
            }

            // 构建树
            return BuildMenuTree(completeMenus);
        }

        // 确保父级路径存在
        private void EnsureParentPath(List<MenuDto> menus, int parentId, HashSet<int> accessibleIds)
        {
            if (accessibleIds.Contains(parentId))
                return;

            var parentMenu = _context.Menus.FirstOrDefault(m => m.Id == parentId);
            if (parentMenu != null)
            {
                var parentDto = new MenuDto
                {
                    Id = parentMenu.Id,
                    Name = parentMenu.Name,
                    Path = parentMenu.Path,
                    ComponentPath = parentMenu.ComponentPath,
                    Icon = parentMenu.Icon,
                    ParentId = parentMenu.ParentId,
                    Order = parentMenu.Order,
                    PermissionCode = parentMenu.PermissionCode,
                    IsVisible = parentMenu.IsVisible
                };

                menus.Add(parentDto);
                accessibleIds.Add(parentId);

                if (parentMenu.ParentId.HasValue)
                {
                    EnsureParentPath(menus, parentMenu.ParentId.Value, accessibleIds);
                }
            }
        }
    }
}