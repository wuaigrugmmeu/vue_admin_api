using Microsoft.EntityFrameworkCore;
using UserPermissionSystem.Domain.Entities;
using UserPermissionSystem.Domain.Events;

namespace UserPermissionSystem.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Menu> Menus { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 忽略 DomainEvent 类型，不要将其视为数据库实体
            modelBuilder.Ignore<DomainEvent>();

            // 配置 Permission 实体使用 Code 作为主键
            modelBuilder.Entity<Permission>()
                .HasKey(p => p.Code);

            // 配置用户角色关联
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // 配置角色权限关联
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionCode });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionCode);

            // 菜单和权限关联
            modelBuilder.Entity<Menu>()
                .HasOne(m => m.Permission)
                .WithMany(p => p.Menus)
                .HasForeignKey(m => m.PermissionCode);
        }
    }
}