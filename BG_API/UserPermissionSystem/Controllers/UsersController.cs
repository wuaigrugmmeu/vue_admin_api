using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserPermissionSystem.Infrastructure.Persistence;
using UserPermissionSystem.Infrastructure.Caching;
using UserPermissionSystem.DTOs;
using UserPermissionSystem.Domain.Aggregates.UserAggregate;
using UserPermissionSystem.Domain.Aggregates.RoleAggregate;
using UserPermissionSystem.Application.Interfaces;

namespace UserPermissionSystem.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly ICacheService _cacheService;
        private readonly CacheManager _cacheManager;

        public UsersController(
            AppDbContext context, 
            IAuthService authService, 
            ICacheService cacheService,
            CacheManager cacheManager)
        {
            _context = context;
            _authService = authService;
            _cacheService = cacheService;
            _cacheManager = cacheManager;
        }

        [HttpGet]
        [Authorize(Policy = "RequirePermission:user:list")]
        public async Task<ActionResult<PagedResultDto<UserListDto>>> GetUsers([FromQuery] UserPagedRequestDto request)
        {
            // 生成缓存键，包含分页和过滤参数
            var cacheKey = CacheKeys.Query(
                CacheKeys.UserList(), 
                request.Page, 
                request.PageSize, 
                request.Search ?? "null", 
                request.IsActive?.ToString() ?? "null"
            );

            // 从缓存获取数据，如果缓存不存在则从数据库查询并缓存结果
            var result = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () => {
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

                    return new PagedResultDto<UserListDto>
                    {
                        Items = userDtos,
                        TotalCount = totalCount
                    };
                },
                // 设置缓存超时为5分钟
                expireTime: 5
            );

            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "RequirePermission:user:read")]
        public async Task<ActionResult<UserDetailDto>> GetUser(int id)
        {
            // 使用缓存获取单个用户详情
            var userDto = await _cacheService.GetOrCreateAsync(
                CacheKeys.UserById(id),
                async () => {
                    var user = await _context.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.Id == id);

                    if (user == null)
                    {
                        return null;
                    }

                    var roles = user.UserRoles
                        .Select(ur => new RoleDto
                        {
                            Id = ur.Role.Id,
                            Name = ur.Role.Name,
                            Description = ur.Role.Description
                        })
                        .ToList();

                    return new UserDetailDto
                    {
                        Id = user.Id,
                        Username = user.UserName,
                        Email = user.Email,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        UpdatedAt = user.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                        Roles = roles,
                        RowVersion = user.RowVersion // 添加RowVersion
                    };
                },
                // 设置缓存超时为10分钟
                expireTime: 10
            );

            if (userDto == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

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

            // 使用User聚合根的工厂方法创建用户
            var user = Domain.Aggregates.UserAggregate.User.Create(
                createUserDto.Username, 
                createUserDto.Password, 
                createUserDto.Email ?? string.Empty,
                null, // displayName 使用默认值
                null  // phoneNumber 使用默认值
            );

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 如果提供了角色，则分配角色
            if (createUserDto.RoleIds != null && createUserDto.RoleIds.Any())
            {
                var roles = await _context.Roles
                    .Where(r => createUserDto.RoleIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var role in roles)
                {
                    user.AssignRoleById(role.Id); // 使用AssignRoleById而不是AssignRole，避免类型转换错误
                }

                await _context.SaveChangesAsync();
            }

            // 使用缓存管理器清除相关缓存
            await _cacheManager.InvalidateUserCacheAsync(user.Id);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, null);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequirePermission:user:update")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                // 更新用户信息，使用User实体方法
                user.UpdateInfo(
                    updateUserDto.Email ?? user.Email, // 如果没有提供新值，则保留原值
                    null, // displayName 保持原值
                    null  // phoneNumber 保持原值
                );

                // 更新用户状态
                if (updateUserDto.IsActive.HasValue)
                {
                    if (updateUserDto.IsActive.Value)
                    {
                        user.Activate();
                    }
                    else
                    {
                        user.Deactivate();
                    }
                }

                // 添加并发控制标记到实体追踪中
                _context.Entry(user).Property("RowVersion").OriginalValue = updateUserDto.RowVersion;

                await _context.SaveChangesAsync();

                // 使用缓存管理器清除相关缓存
                await _cacheManager.InvalidateUserCacheAsync(id);

                // 返回更新后的用户及新的RowVersion
                var updatedUser = await _context.Users.FindAsync(id);
                return Ok(new { 
                    message = "用户更新成功",
                    rowVersion = updatedUser.RowVersion 
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // 发生并发冲突
                var entry = ex.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync();
                
                if (databaseValues == null)
                {
                    // 如果数据库中的实体已被删除
                    return NotFound(new { message = "用户已被其他用户删除，无法更新" });
                }
                
                // 获取数据库中当前值
                var databaseUser = databaseValues.ToObject() as User;
                
                return Conflict(new { 
                    message = "用户数据已被其他用户修改，请刷新后重试",
                    currentData = new {
                        id = databaseUser.Id,
                        username = databaseUser.UserName,
                        email = databaseUser.Email,
                        isActive = databaseUser.IsActive,
                        updatedAt = databaseUser.UpdatedAt,
                        rowVersion = databaseUser.RowVersion
                    }
                });
            }
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

            // 使用缓存管理器清除相关缓存
            await _cacheManager.InvalidateUserCacheAsync(id);

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

            // 获取所有要分配的角色
            var roles = await _context.Roles
                .Where(r => roleAssignmentDto.RoleIds.Contains(r.Id))
                .ToListAsync();

            // 清除现有角色（首先获取当前用户的所有角色ID）
            var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
            foreach (var roleId in currentRoleIds)
            {
                user.RemoveRole(roleId);
            }

            // 添加新的角色 - 这里需要修改调用方式
            foreach (var role in roles)
            {
                // 直接使用角色ID进行分配，避免类型转换问题
                user.AssignRoleById(role.Id);
            }

            await _context.SaveChangesAsync();

            // 使用缓存管理器清除相关缓存
            await _cacheManager.InvalidateUserCacheAsync(id);

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

            // 使用缓存管理器清除相关缓存
            await _cacheManager.InvalidateUserCacheAsync(id);

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