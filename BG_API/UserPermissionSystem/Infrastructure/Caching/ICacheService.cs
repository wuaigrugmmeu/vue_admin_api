using System;
using System.Threading.Tasks;

namespace UserPermissionSystem.Infrastructure.Caching
{
    public interface ICacheService
    {
        /// <summary>
        /// 获取缓存，如果缓存不存在则执行factory函数并缓存结果
        /// </summary>
        /// <typeparam name="T">缓存对象类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">获取数据的回调函数</param>
        /// <param name="expireTime">过期时间（分钟）</param>
        /// <returns>缓存数据</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int expireTime = 30);

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <typeparam name="T">缓存对象类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存数据，若不存在则为默认值</returns>
        Task<T> GetAsync<T>(string key);

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <typeparam name="T">缓存对象类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expireTime">过期时间（分钟）</param>
        Task SetAsync<T>(string key, T value, int expireTime = 30);

        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        Task RemoveAsync(string key);

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        Task ClearAllAsync();
        
        /// <summary>
        /// 根据前缀删除缓存
        /// </summary>
        /// <param name="prefix">缓存键前缀</param>
        Task RemoveByPrefixAsync(string prefix);
    }
}