using System.Threading.Tasks;
using UserPermissionSystem.Domain.Entities;

namespace UserPermissionSystem.Domain.Services
{
    /// <summary>
    /// 认证领域服务接口
    /// </summary>
    public interface IAuthDomainService
    {
        /// <summary>
        /// 验证用户凭据
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>如果验证成功，返回用户实体；否则返回null</returns>
        Task<User> ValidateUserCredentialsAsync(string userName, string password);
        
        /// <summary>
        /// 生成访问令牌
        /// </summary>
        /// <param name="user">用户实体</param>
        /// <returns>JWT令牌</returns>
        Task<string> GenerateAccessTokenAsync(User user);
        
        /// <summary>
        /// 获取用户所有权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>权限集合</returns>
        Task<string[]> GetUserPermissionsAsync(int userId);

        /// <summary>
        /// 验证用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="password">密码</param>
        /// <returns>密码是否有效</returns>
        Task<bool> ValidateUserPasswordAsync(int userId, string password);

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="currentPassword">当前密码</param>
        /// <param name="newPassword">新密码</param>
        /// <returns>是否成功</returns>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newPassword">新密码</param>
        /// <returns>是否成功</returns>
        Task<bool> ResetPasswordAsync(int userId, string newPassword);
    }
}