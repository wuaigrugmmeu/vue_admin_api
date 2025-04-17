using System;
using System.Text;

namespace UserPermissionSystem.Infrastructure.Caching
{
    /// <summary>
    /// 缓存键生成器，用于统一管理缓存键
    /// </summary>
    public static class CacheKeys
    {
        // 缓存前缀，用于区分不同模块
        public const string UserPrefix = "User";
        public const string RolePrefix = "Role";
        public const string PermissionPrefix = "Permission";
        public const string MenuPrefix = "Menu";
        
        // 用户相关缓存键
        public static string UserById(int id) => $"{UserPrefix}:Id:{id}";
        public static string UserByUsername(string username) => $"{UserPrefix}:Username:{username}";
        public static string UserList() => $"{UserPrefix}:List";
        public static string UserRoles(int userId) => $"{UserPrefix}:Roles:{userId}";
        
        // 角色相关缓存键
        public static string RoleById(int id) => $"{RolePrefix}:Id:{id}";
        public static string RoleByName(string name) => $"{RolePrefix}:Name:{name}";
        public static string RoleList() => $"{RolePrefix}:List";
        public static string RolePermissions(int roleId) => $"{RolePrefix}:Permissions:{roleId}";
        
        // 权限相关缓存键
        public static string PermissionById(int id) => $"{PermissionPrefix}:Id:{id}";
        public static string PermissionList() => $"{PermissionPrefix}:List";
        public static string UserPermissions(int userId) => $"{UserPrefix}:Permissions:{userId}";
        
        // 菜单相关缓存键
        public static string MenuById(int id) => $"{MenuPrefix}:Id:{id}";
        public static string MenuList() => $"{MenuPrefix}:List";
        public static string UserMenus(int userId) => $"{UserPrefix}:Menus:{userId}";
        
        /// <summary>
        /// 生成查询缓存键
        /// </summary>
        /// <param name="prefix">前缀</param>
        /// <param name="parameters">查询参数</param>
        /// <returns>缓存键</returns>
        public static string Query(string prefix, params object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                return $"{prefix}:Query";
            }
            
            var sb = new StringBuilder($"{prefix}:Query");
            foreach (var param in parameters)
            {
                if (param != null)
                {
                    sb.Append($":{param}");
                }
            }
            
            return sb.ToString();
        }
    }
}