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
using UserPermissionSystem.Application.Interfaces;
using UserPermissionSystem.Infrastructure.Services;
using UserPermissionSystem.Domain.Interfaces;
using UserPermissionSystem.Domain.Services;
using UserPermissionSystem.Infrastructure.Authentication;
using UserPermissionSystem.Infrastructure.Persistence;
using UserPermissionSystem.Infrastructure.Seeders;

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

                // 使用DatabaseSeeder初始化系统数据
                await DatabaseSeeder.SeedAsync(dbContext);
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

            // 注册基础设施层服务
            builder.Services.AddScoped<DbContext, AppDbContext>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 注册应用层服务
            builder.Services.AddScoped<IAuthDomainService, AuthDomainService>();
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
                c.SwaggerDoc("v1", new OpenApiInfo
                {
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
    }
}
