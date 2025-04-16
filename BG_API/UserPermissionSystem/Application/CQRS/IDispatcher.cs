using System.Threading;
using System.Threading.Tasks;

namespace UserPermissionSystem.Application.CQRS
{
    /// <summary>
    /// 命令和查询分发器接口
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// 发送命令
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <param name="command">命令实例</param>
        /// <param name="cancellationToken">取消标记</param>
        Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand;
            
        /// <summary>
        /// 发送带返回值的命令
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="command">命令实例</param>
        /// <param name="cancellationToken">取消标记</param>
        /// <returns>操作结果</returns>
        Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>;
            
        /// <summary>
        /// 发送查询
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="query">查询实例</param>
        /// <param name="cancellationToken">取消标记</param>
        /// <returns>查询结果</returns>
        Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>;
    }
}