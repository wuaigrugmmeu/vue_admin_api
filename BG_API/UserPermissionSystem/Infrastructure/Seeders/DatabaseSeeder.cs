using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserPermissionSystem.Domain.Entities;
using UserPermissionSystem.Infrastructure.Persistence;

namespace UserPermissionSystem.Infrastructure.Seeders
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext dbContext)
        {
            // 检查是否已经有数据
            if (await dbContext.Users.AnyAsync() || await dbContext.Roles.AnyAsync() || await dbContext.Permissions.AnyAsync())
            {
                return; // 已经初始化过，不再重复
            }

            // 确保按正确的顺序添加数据：
            // 1. 先添加权限（Permissions）
            // 2. 再添加角色（Roles）并分配权限
            // 3. 然后添加用户（Users）并分配角色
            // 4. 最后添加菜单（Menus），因为它们依赖于权限
            await SeedPermissionsAsync(dbContext);
            await dbContext.SaveChangesAsync();
            
            await SeedRolesAsync(dbContext);
            await dbContext.SaveChangesAsync();
            
            await SeedUsersAsync(dbContext);
            await dbContext.SaveChangesAsync();
            
            await SeedMenusAsync(dbContext);
            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedPermissionsAsync(AppDbContext dbContext)
        {
            var permissions = new List<Permission>
            {
                // 用户管理权限
                Permission.Create("user:list", "用户列表", "查看用户列表", "用户管理", PermissionType.Api),
                Permission.Create("user:read", "查看用户", "查看用户详情", "用户管理", PermissionType.Api),
                Permission.Create("user:create", "创建用户", "创建新用户", "用户管理", PermissionType.Api),
                Permission.Create("user:update", "更新用户", "更新用户信息", "用户管理", PermissionType.Api),
                Permission.Create("user:delete", "删除用户", "删除用户", "用户管理", PermissionType.Api),
                Permission.Create("user:assignRoles", "分配角色", "为用户分配角色", "用户管理", PermissionType.Api),
                Permission.Create("user:resetPassword", "重置密码", "重置用户密码", "用户管理", PermissionType.Api),
                
                // 角色管理权限
                Permission.Create("role:list", "角色列表", "查看角色列表", "角色管理", PermissionType.Api),
                Permission.Create("role:read", "查看角色", "查看角色详情", "角色管理", PermissionType.Api),
                Permission.Create("role:create", "创建角色", "创建新角色", "角色管理", PermissionType.Api),
                Permission.Create("role:update", "更新角色", "更新角色信息", "角色管理", PermissionType.Api),
                Permission.Create("role:delete", "删除角色", "删除角色", "角色管理", PermissionType.Api),
                Permission.Create("role:assignPermissions", "分配权限", "为角色分配权限", "角色管理", PermissionType.Api),
                
                // 权限管理权限
                Permission.Create("permission:list", "权限列表", "查看权限列表", "权限管理", PermissionType.Api),
                
                // 菜单管理权限
                Permission.Create("menu:list", "菜单列表", "查看菜单列表", "菜单管理", PermissionType.Api),
                Permission.Create("menu:read", "查看菜单", "查看菜单详情", "菜单管理", PermissionType.Api),
                Permission.Create("menu:create", "创建菜单", "创建新菜单", "菜单管理", PermissionType.Api),
                Permission.Create("menu:update", "更新菜单", "更新菜单信息", "菜单管理", PermissionType.Api),
                Permission.Create("menu:delete", "删除菜单", "删除菜单", "菜单管理", PermissionType.Api)
            };

            await dbContext.Permissions.AddRangeAsync(permissions);
            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(AppDbContext dbContext)
        {
            // 创建管理员角色
            var adminRole = Role.Create("管理员", "系统管理员，拥有所有权限");
            
            await dbContext.Roles.AddAsync(adminRole);
            await dbContext.SaveChangesAsync();

            // 为管理员角色分配所有权限
            var permissions = await dbContext.Permissions.ToListAsync();
            foreach (var permission in permissions)
            {
                await dbContext.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionCode = permission.Code
                });
            }

            // 创建普通用户角色
            var userRole = Role.Create("普通用户", "普通用户，拥有基本权限");
            
            await dbContext.Roles.AddAsync(userRole);
            await dbContext.SaveChangesAsync();

            // 为普通用户角色分配基本权限
            var userPermissions = permissions.Where(p => p.Code.StartsWith("user:list") || p.Code.StartsWith("role:list") || p.Code.StartsWith("permission:list"));
            foreach (var permission in userPermissions)
            {
                await dbContext.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = userRole.Id,
                    PermissionCode = permission.Code
                });
            }

            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedUsersAsync(AppDbContext dbContext)
        {
            // 创建初始管理员用户
            var adminUser = User.Create(
                userName: "admin", 
                password: "admin123", 
                email: "admin@example.com", 
                displayName: "系统管理员",
                phoneNumber: "13800000000"
            );

            // 确保密码被正确设置
            adminUser.SetPassword("admin123");
            
            await dbContext.Users.AddAsync(adminUser);
            await dbContext.SaveChangesAsync();

            // 为管理员用户分配管理员角色
            var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "管理员");
            if (adminRole != null)
            {
                adminUser.AssignRole(adminRole);
                await dbContext.SaveChangesAsync();
            }
        }

        private static async Task SeedMenusAsync(AppDbContext dbContext)
        {
            // 创建初始菜单
            var menus = new List<Menu>
            {
                // 系统管理
                Menu.Create(
                    name: "系统管理", 
                    path: "/system", 
                    componentPath: "Layout", 
                    icon: "setting", 
                    parentId: null, 
                    order: 1, 
                    permissionCode: null,  // 修改为null，而不是空字符串
                    isVisible: true)
            };

            await dbContext.Menus.AddRangeAsync(menus);
            await dbContext.SaveChangesAsync();
            
            // 获取系统管理菜单ID
            var systemMenu = await dbContext.Menus.FirstOrDefaultAsync(m => m.Name == "系统管理");
            if (systemMenu == null) return;
            
            var subMenus = new List<Menu>
            {
                // 用户管理菜单
                Menu.Create(
                    name: "用户管理", 
                    path: "users", 
                    componentPath: "system/users/index", 
                    icon: "user", 
                    parentId: systemMenu.Id, 
                    order: 1, 
                    permissionCode: "user:list", 
                    isVisible: true),
                // 角色管理菜单
                Menu.Create(
                    name: "角色管理", 
                    path: "roles", 
                    componentPath: "system/roles/index", 
                    icon: "peoples", 
                    parentId: systemMenu.Id, 
                    order: 2, 
                    permissionCode: "role:list", 
                    isVisible: true),
                // 菜单管理
                Menu.Create(
                    name: "菜单管理", 
                    path: "menus", 
                    componentPath: "system/menus/index", 
                    icon: "tree-table", 
                    parentId: systemMenu.Id, 
                    order: 3, 
                    permissionCode: "menu:list", 
                    isVisible: true)
            };

            await dbContext.Menus.AddRangeAsync(subMenus);
            await dbContext.SaveChangesAsync();
        }
    }
}