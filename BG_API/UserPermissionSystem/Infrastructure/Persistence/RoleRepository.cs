using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserPermissionSystem.Domain.Aggregates.RoleAggregate;
using UserPermissionSystem.Domain.Interfaces;

namespace UserPermissionSystem.Infrastructure.Persistence
{
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context)
        {
        }
        
        public IUnitOfWork UnitOfWork => _unitOfWork;

        public async Task<Role> FindByNameAsync(string roleName)
        {
            return await _context.Set<Role>()
                .SingleOrDefaultAsync(r => r.Name == roleName);
        }

        public async Task<IEnumerable<string>> GetRolePermissionsAsync(int roleId)
        {
            var permissions = await _context.Set<Role>()
                .Where(r => r.Id == roleId)
                .SelectMany(r => r.RolePermissions)
                .Select(rp => rp.PermissionCode)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        public async Task<bool> IsNameUniqueAsync(string roleName)
        {
            return !await _context.Set<Role>().AnyAsync(r => r.Name == roleName);
        }
    }
}