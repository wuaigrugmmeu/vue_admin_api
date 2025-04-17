using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UserPermissionSystem.Infrastructure.Caching
{
    public class CacheManager
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheManager> _logger;

        public CacheManager(ICacheService cacheService, ILogger<CacheManager> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// 当用户数据变更时，清除与该用户相关的所有缓存
        /// </summary>
        public async Task InvalidateUserCacheAsync(int userId)
        {
            _logger.LogInformation("正在清除用户ID={UserId}的所有相关缓存", userId);
            
            var tasks = new List<Task>
            {
                _cacheService.RemoveAsync(CacheKeys.UserById(userId)),
                _cacheService.RemoveAsync(CacheKeys.UserRoles(userId)),
                _cacheService.RemoveAsync(CacheKeys.UserPermissions(userId)),
                _cacheService.RemoveAsync(CacheKeys.UserMenus(userId)),
                // 清除所有可能包含该用户的列表缓存
                _cacheService.RemoveByPrefixAsync(CacheKeys.UserList())
            };
            
            await Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// 当角色数据变更时，清除与该角色相关的所有缓存
        /// </summary>
        public async Task InvalidateRoleCacheAsync(int roleId)
        {
            _logger.LogInformation("正在清除角色ID={RoleId}的所有相关缓存", roleId);
            
            var tasks = new List<Task>
            {
                _cacheService.RemoveAsync(CacheKeys.RoleById(roleId)),
                _cacheService.RemoveAsync(CacheKeys.RolePermissions(roleId)),
                // 清除所有可能包含该角色的列表缓存
                _cacheService.RemoveByPrefixAsync(CacheKeys.RoleList()),
                // 清除所有用户的权限和菜单缓存，因为用户权限可能受角色变更影响
                _cacheService.RemoveByPrefixAsync($"{CacheKeys.UserPrefix}:Permissions"),
                _cacheService.RemoveByPrefixAsync($"{CacheKeys.UserPrefix}:Menus"),
                // 清除所有用户列表，因为可能包含角色信息
                _cacheService.RemoveByPrefixAsync(CacheKeys.UserList())
            };
            
            await Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// 当权限数据变更时，清除与权限相关的所有缓存
        /// </summary>
        public async Task InvalidatePermissionCacheAsync(int permissionId)
        {
            _logger.LogInformation("正在清除权限ID={PermissionId}的所有相关缓存", permissionId);
            
            var tasks = new List<Task>
            {
                _cacheService.RemoveAsync(CacheKeys.PermissionById(permissionId)),
                _cacheService.RemoveByPrefixAsync(CacheKeys.PermissionList()),
                // 清除所有角色的权限缓存
                _cacheService.RemoveByPrefixAsync($"{CacheKeys.RolePrefix}:Permissions"),
                // 清除所有用户的权限缓存
                _cacheService.RemoveByPrefixAsync($"{CacheKeys.UserPrefix}:Permissions")
            };
            
            await Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// 当菜单数据变更时，清除与菜单相关的所有缓存
        /// </summary>
        public async Task InvalidateMenuCacheAsync(int menuId)
        {
            _logger.LogInformation("正在清除菜单ID={MenuId}的所有相关缓存", menuId);
            
            var tasks = new List<Task>
            {
                _cacheService.RemoveAsync(CacheKeys.MenuById(menuId)),
                _cacheService.RemoveByPrefixAsync(CacheKeys.MenuList()),
                // 清除所有用户的菜单缓存
                _cacheService.RemoveByPrefixAsync($"{CacheKeys.UserPrefix}:Menus")
            };
            
            await Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// 清除所有缓存（例如在大规模数据更新后）
        /// </summary>
        public Task ClearAllCacheAsync()
        {
            _logger.LogWarning("正在清除所有缓存数据");
            return _cacheService.ClearAllAsync();
        }
    }
}