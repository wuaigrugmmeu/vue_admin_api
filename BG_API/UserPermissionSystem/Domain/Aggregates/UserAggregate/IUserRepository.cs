using System.Collections.Generic;
using System.Threading.Tasks;
using UserPermissionSystem.Domain.Interfaces;

namespace UserPermissionSystem.Domain.Aggregates.UserAggregate
{
    /// <summary>
    /// 用户聚合根的专用仓储接口
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        IUnitOfWork UnitOfWork { get; }
        Task<User> FindByUserNameAsync(string userName);
        Task<User> FindByEmailAsync(string email);
        Task<IEnumerable<string>> GetUserPermissionsAsync(int userId);
        Task<bool> IsUserNameUniqueAsync(string userName);
        Task<bool> IsEmailUniqueAsync(string email);
    }
}