using System;
using System.Collections.Generic;

namespace UserPermissionSystem.DTOs
{
    /// <summary>
    /// 登录请求DTO
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 登录响应DTO
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// 用户显示名
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// JWT令牌
        /// </summary>
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// 用户拥有的权限编码列表
        /// </summary>
        public string[] Permissions { get; set; } = Array.Empty<string>();
    }
    
    /// <summary>
    /// 用户信息DTO
    /// </summary>
    public class UserInfoDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// 用户显示名
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// 电子邮件
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// 手机号码
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;
        
        /// <summary>
        /// 用户角色ID列表
        /// </summary>
        public List<int> RoleIds { get; set; } = new List<int>();
        
        /// <summary>
        /// 用户角色名称列表
        /// </summary>
        public List<string> RoleNames { get; set; } = new List<string>();
        
        /// <summary>
        /// 用户权限编码列表
        /// </summary>
        public string[] Permissions { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }
    }
    
    /// <summary>
    /// 修改密码请求DTO
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>
        /// 旧密码
        /// </summary>
        public string OldPassword { get; set; } = string.Empty;
        
        /// <summary>
        /// 新密码
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
        
        /// <summary>
        /// 确认新密码
        /// </summary>
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 重置密码请求DTO
    /// </summary>
    public class ResetPasswordDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// 新密码
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
        
        /// <summary>
        /// 确认新密码
        /// </summary>
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}