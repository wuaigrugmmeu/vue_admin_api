using System.Collections.Generic;
using System.Threading.Tasks;
using UserPermissionSystem.Domain.Interfaces;

namespace UserPermissionSystem.Domain.Aggregates.RoleAggregate
{
    /// <summary>
    /// 角色聚合根的专用仓储接口
    /// </summary>
    public interface IRoleRepository : IRepository<Role>
    {
        IUnitOfWork UnitOfWork { get; }
        Task<Role> FindByNameAsync(string roleName);
        Task<IEnumerable<string>> GetRolePermissionsAsync(int roleId);
        Task<bool> IsNameUniqueAsync(string roleName);
    }
}