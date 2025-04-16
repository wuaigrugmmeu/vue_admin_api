using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserPermissionSystem.Data;
using UserPermissionSystem.Models;
using UserPermissionSystem.Services;

namespace UserPermissionSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureServices(builder);

            var app = builder.Build();

            ConfigureMiddleware(app);

            // 应用程序启动时可以进行数据库初始化
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var dbContext = services.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();

                // 初始化系统数据
                await SeedData(dbContext);
            }

            app.Run();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // 数据库配置
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));

            // 配置CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policyBuilder =>
                {
                    policyBuilder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            // 配置JWT认证
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            builder.Services.Configure<JwtSettings>(jwtSettings);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecurityKey"] ?? "DefaultKeyForDevelopment12345678901234"))
                };
            });

            // 配置授权策略
            ConfigureAuthorizationPolicies(builder.Services);

            // 注册服务
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddControllers();

            // 配置Swagger
            ConfigureSwagger(builder.Services);
        }

        private static void ConfigureAuthorizationPolicies(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // 添加基于权限的策略，格式为"RequirePermission:{permissionCode}"
                options.AddPolicy("RequirePermission:user:list", policy => policy.RequireClaim("permission", "user:list"));
                options.AddPolicy("RequirePermission:user:read", policy => policy.RequireClaim("permission", "user:read"));
                options.AddPolicy("RequirePermission:user:create", policy => policy.RequireClaim("permission", "user:create"));
                options.AddPolicy("RequirePermission:user:update", policy => policy.RequireClaim("permission", "user:update"));
                options.AddPolicy("RequirePermission:user:delete", policy => policy.RequireClaim("permission", "user:delete"));
                options.AddPolicy("RequirePermission:user:assignRoles", policy => policy.RequireClaim("permission", "user:assignRoles"));
                options.AddPolicy("RequirePermission:user:resetPassword", policy => policy.RequireClaim("permission", "user:resetPassword"));

                options.AddPolicy("RequirePermission:role:list", policy => policy.RequireClaim("permission", "role:list"));
                options.AddPolicy("RequirePermission:role:read", policy => policy.RequireClaim("permission", "role:read"));
                options.AddPolicy("RequirePermission:role:create", policy => policy.RequireClaim("permission", "role:create"));
                options.AddPolicy("RequirePermission:role:update", policy => policy.RequireClaim("permission", "role:update"));
                options.AddPolicy("RequirePermission:role:delete", policy => policy.RequireClaim("permission", "role:delete"));
                options.AddPolicy("RequirePermission:role:assignPermissions", policy => policy.RequireClaim("permission", "role:assignPermissions"));

                options.AddPolicy("RequirePermission:permission:list", policy => policy.RequireClaim("permission", "permission:list"));
                
                options.AddPolicy("RequirePermission:menu:list", policy => policy.RequireClaim("permission", "menu:list"));
                options.AddPolicy("RequirePermission:menu:read", policy => policy.RequireClaim("permission", "menu:read"));
                options.AddPolicy("RequirePermission:menu:create", policy => policy.RequireClaim("permission", "menu:create"));
                options.AddPolicy("RequirePermission:menu:update", policy => policy.RequireClaim("permission", "menu:update"));
                options.AddPolicy("RequirePermission:menu:delete", policy => policy.RequireClaim("permission", "menu:delete"));
            });
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { 
                    Title = "User Permission System API", 
                    Version = "v1",
                    Description = @"
## 认证说明
1. 先调用 `/api/Auth/login` 接口获取 token (默认用户名: admin, 密码: admin123)
2. 点击右上角 'Authorize' 按钮
3. 在弹出的窗口中，输入 'Bearer ' + 你获取到的 token，例如: Bearer eyJhbGciOiJIUzI1NiIsInR5...
4. 点击 'Authorize' 按钮完成授权
5. 现在你可以访问需要认证的API了
"
                });
                
                // 添加JWT认证支持
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT授权，使用 'Bearer ' + token。例如: \"Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5...\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
                
                // 自定义Swagger文档顺序，把认证相关接口放在最前面
                c.OrderActionsBy(apiDesc => 
                    apiDesc.ActionDescriptor.RouteValues["controller"].Equals("Auth", StringComparison.OrdinalIgnoreCase) ? "0" : 
                    apiDesc.ActionDescriptor.RouteValues["controller"]);
            });
        }

        private static void ConfigureMiddleware(WebApplication app)
        {
            // 配置HTTP请求管道
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                // 即使在生产环境中也启用Swagger
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // 添加根路径重定向到Swagger
            app.MapGet("/", () => Results.Redirect("/swagger"));

            app.UseHttpsRedirection();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
        }

        // 数据初始化方法
        private static async Task SeedData(AppDbContext dbContext)
        {
            // 检查是否已经有数据
            if (await dbContext.Users.AnyAsync() || await dbContext.Roles.AnyAsync() || await dbContext.Permissions.AnyAsync())
            {
                return; // 已经初始化过，不再重复
            }

            // 创建基本权限
            var permissions = new List<Permission>
            {
                // 用户管理权限
                new Permission { Code = "user:list", Name = "用户列表", Description = "查看用户列表", Module = "用户管理", Type = PermissionType.Api },
                new Permission { Code = "user:read", Name = "查看用户", Description = "查看用户详情", Module = "用户管理", Type = PermissionType.Api },
                new Permission { Code = "user:create", Name = "创建用户", Description = "创建新用户", Module = "用户管理", Type = PermissionType.Api },
                new Permission { Code = "user:update", Name = "更新用户", Description = "更新用户信息", Module = "用户管理", Type = PermissionType.Api },
                new Permission { Code = "user:delete", Name = "删除用户", Description = "删除用户", Module = "用户管理", Type = PermissionType.Api },
                new Permission { Code = "user:assignRoles", Name = "分配角色", Description = "为用户分配角色", Module = "用户管理", Type = PermissionType.Api },
                new Permission { Code = "user:resetPassword", Name = "重置密码", Description = "重置用户密码", Module = "用户管理", Type = PermissionType.Api },
                
                // 角色管理权限
                new Permission { Code = "role:list", Name = "角色列表", Description = "查看角色列表", Module = "角色管理", Type = PermissionType.Api },
                new Permission { Code = "role:read", Name = "查看角色", Description = "查看角色详情", Module = "角色管理", Type = PermissionType.Api },
                new Permission { Code = "role:create", Name = "创建角色", Description = "创建新角色", Module = "角色管理", Type = PermissionType.Api },
                new Permission { Code = "role:update", Name = "更新角色", Description = "更新角色信息", Module = "角色管理", Type = PermissionType.Api },
                new Permission { Code = "role:delete", Name = "删除角色", Description = "删除角色", Module = "角色管理", Type = PermissionType.Api },
                new Permission { Code = "role:assignPermissions", Name = "分配权限", Description = "为角色分配权限", Module = "角色管理", Type = PermissionType.Api },
                
                // 权限管理权限
                new Permission { Code = "permission:list", Name = "权限列表", Description = "查看权限列表", Module = "权限管理", Type = PermissionType.Api },
                
                // 菜单管理权限
                new Permission { Code = "menu:list", Name = "菜单列表", Description = "查看菜单列表", Module = "菜单管理", Type = PermissionType.Api },
                new Permission { Code = "menu:read", Name = "查看菜单", Description = "查看菜单详情", Module = "菜单管理", Type = PermissionType.Api },
                new Permission { Code = "menu:create", Name = "创建菜单", Description = "创建新菜单", Module = "菜单管理", Type = PermissionType.Api },
                new Permission { Code = "menu:update", Name = "更新菜单", Description = "更新菜单信息", Module = "菜单管理", Type = PermissionType.Api },
                new Permission { Code = "menu:delete", Name = "删除菜单", Description = "删除菜单", Module = "菜单管理", Type = PermissionType.Api }
            };

            await dbContext.Permissions.AddRangeAsync(permissions);

            // 创建管理员角色
            var adminRole = new Role 
            { 
                Name = "管理员", 
                Description = "系统管理员，拥有所有权限", 
                CreatedAt = DateTime.UtcNow 
            };
            
            await dbContext.Roles.AddAsync(adminRole);
            await dbContext.SaveChangesAsync();

            // 为管理员角色分配所有权限
            foreach (var permission in permissions)
            {
                await dbContext.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionCode = permission.Code
                });
            }

            // 创建普通用户角色
            var userRole = new Role 
            { 
                Name = "普通用户", 
                Description = "普通用户，拥有基本权限", 
                CreatedAt = DateTime.UtcNow 
            };
            
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

            // 创建初始管理员用户
            var adminUser = new User
            {
                UserName = "admin",
                // SHA256哈希密码 "admin123"
                PasswordHash = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=", // admin123 的SHA256哈希
                Email = "admin@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            await dbContext.Users.AddAsync(adminUser);
            await dbContext.SaveChangesAsync();

            // 为管理员用户分配管理员角色
            await dbContext.UserRoles.AddAsync(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });

            // 创建初始菜单
            var menus = new List<Menu>
            {
                // 系统管理
                new Menu 
                { 
                    Name = "系统管理", 
                    Path = "/system", 
                    ComponentPath = "Layout", 
                    Icon = "setting", 
                    Order = 1, 
                    IsVisible = true 
                },
                // 用户管理菜单
                new Menu 
                { 
                    Name = "用户管理", 
                    Path = "users", 
                    ComponentPath = "system/users/index", 
                    Icon = "user", 
                    ParentId = 1, // 系统管理下的子菜单
                    Order = 1, 
                    PermissionCode = "user:list",
                    IsVisible = true 
                },
                // 角色管理菜单
                new Menu 
                { 
                    Name = "角色管理", 
                    Path = "roles", 
                    ComponentPath = "system/roles/index", 
                    Icon = "peoples", 
                    ParentId = 1, // 系统管理下的子菜单
                    Order = 2, 
                    PermissionCode = "role:list",
                    IsVisible = true 
                },
                // 菜单管理
                new Menu 
                { 
                    Name = "菜单管理", 
                    Path = "menus", 
                    ComponentPath = "system/menus/index", 
                    Icon = "tree-table", 
                    ParentId = 1, // 系统管理下的子菜单
                    Order = 3, 
                    PermissionCode = "menu:list",
                    IsVisible = true 
                }
            };

            await dbContext.Menus.AddRangeAsync(menus);
            await dbContext.SaveChangesAsync();
        }
    }
}
