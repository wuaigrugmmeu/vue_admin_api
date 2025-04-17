using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserPermissionSystem.Domain.Aggregates.UserAggregate;
using UserPermissionSystem.Domain.Interfaces;

namespace UserPermissionSystem.Infrastructure.Persistence
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }
        
        public IUnitOfWork UnitOfWork => _unitOfWork;

        public async Task<User> FindByUserNameAsync(string userName)
        {
            return await _context.Set<User>()
                .SingleOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<User> FindByEmailAsync(string email)
        {
            return await _context.Set<User>()
                .SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<string>> GetUserPermissionsAsync(int userId)
        {
            // 获取用户的所有权限
            var permissions = await _context.Set<User>()
                .Where(u => u.Id == userId)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        public async Task<bool> IsUserNameUniqueAsync(string userName)
        {
            return !await _context.Set<User>().AnyAsync(u => u.UserName == userName);
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            return !await _context.Set<User>().AnyAsync(u => u.Email == email);
        }
    }
}