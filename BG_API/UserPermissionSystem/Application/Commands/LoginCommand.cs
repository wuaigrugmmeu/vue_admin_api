using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using UserPermissionSystem.Application.CQRS;
using UserPermissionSystem.Domain.Services;
using UserPermissionSystem.DTOs;

namespace UserPermissionSystem.Application.Commands
{
    /// <summary>
    /// 用户登录命令
    /// </summary>
    public class LoginCommand : ICommand<BaseResponse<LoginResponseDto>>
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 用户登录命令处理器
    /// </summary>
    public class LoginCommandHandler : ICommandHandler<LoginCommand, BaseResponse<LoginResponseDto>>
    {
        private readonly IAuthDomainService _authDomainService;
        
        public LoginCommandHandler(IAuthDomainService authDomainService)
        {
            _authDomainService = authDomainService ?? throw new ArgumentNullException(nameof(authDomainService));
        }
        
        public async Task<BaseResponse<LoginResponseDto>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
        {
            // 数据验证
            if (string.IsNullOrWhiteSpace(command.UserName) || string.IsNullOrWhiteSpace(command.Password))
            {
                return BaseResponse<LoginResponseDto>.Fail("用户名和密码不能为空");
            }
            
            try
            {
                // 验证用户凭据
                var user = await _authDomainService.ValidateUserCredentialsAsync(command.UserName, command.Password);
                
                if (user == null)
                {
                    return BaseResponse<LoginResponseDto>.Fail("用户名或密码不正确");
                }
                
                // 生成访问令牌
                var token = await _authDomainService.GenerateAccessTokenAsync(user);
                
                // 获取用户权限
                var permissions = await _authDomainService.GetUserPermissionsAsync(user.Id);
                
                // 创建响应对象
                var response = new LoginResponseDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    DisplayName = user.DisplayName,
                    Token = token,
                    Permissions = permissions
                };
                
                return BaseResponse<LoginResponseDto>.Ok(response, "登录成功");
            }
            catch (Exception ex)
            {
                return BaseResponse<LoginResponseDto>.Fail($"登录失败: {ex.Message}");
            }
        }
    }
}