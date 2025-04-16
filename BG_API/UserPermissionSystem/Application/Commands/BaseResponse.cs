using System.Collections.Generic;

namespace UserPermissionSystem.Application.Commands
{
    /// <summary>
    /// 命令响应基类
    /// </summary>
    public class BaseResponse
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 消息
        /// </summary>
        public string? Message { get; set; }
        
        /// <summary>
        /// 创建成功响应
        /// </summary>
        /// <param name="message">成功消息</param>
        /// <returns>操作结果对象</returns>
        public static BaseResponse Ok(string message = "操作成功")
        {
            return new BaseResponse
            {
                Success = true,
                Message = message
            };
        }
        
        /// <summary>
        /// 创建失败响应
        /// </summary>
        /// <param name="message">失败消息</param>
        /// <returns>操作结果对象</returns>
        public static BaseResponse Fail(string message = "操作失败")
        {
            return new BaseResponse
            {
                Success = false,
                Message = message
            };
        }
    }

    /// <summary>
    /// 泛型命令响应基类
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    public class BaseResponse<T> : BaseResponse
    {
        /// <summary>
        /// 响应数据
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        /// <param name="data">返回数据</param>
        /// <param name="message">成功消息</param>
        /// <returns>操作结果对象</returns>
        public static BaseResponse<T> Ok(T data, string message = "操作成功")
        {
            return new BaseResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        /// <param name="message">失败消息</param>
        /// <returns>操作结果对象</returns>
        public new static BaseResponse<T> Fail(string message = "操作失败")
        {
            return new BaseResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }

    /// <summary>
    /// 操作结果类
    /// </summary>
    public class OperationResult : BaseResponse
    {
        /// <summary>
        /// 响应数据
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        /// <param name="message">成功消息</param>
        /// <param name="data">返回数据</param>
        /// <returns>操作结果对象</returns>
        public static OperationResult Succeeded(string message = "操作成功", object? data = null)
        {
            return new OperationResult
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        /// <param name="message">失败消息</param>
        /// <param name="data">返回数据</param>
        /// <returns>操作结果对象</returns>
        public static OperationResult Failed(string message = "操作失败", object? data = null)
        {
            return new OperationResult
            {
                Success = false,
                Message = message,
                Data = data
            };
        }
    }

    /// <summary>
    /// 用户信息响应类
    /// </summary>
    public class UserInfoResponse
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = null!;

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 令牌
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// 角色列表
        /// </summary>
        public List<string>? Roles { get; set; }

        /// <summary>
        /// 权限列表
        /// </summary>
        public List<string>? Permissions { get; set; }
    }
}