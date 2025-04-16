using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using UserPermissionSystem.Application.CQRS;
using UserPermissionSystem.Domain.Services;
using UserPermissionSystem.Domain.Interfaces;

namespace UserPermissionSystem.Application.Commands
{
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 修改密码命令
    /// </summary>
    public class ChangePasswordCommand : ICommand<BaseResponse>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// 旧密码
        /// </summary>
        [Required(ErrorMessage = "旧密码不能为空")]
        public string OldPassword { get; set; } = string.Empty;
        
        /// <summary>
        /// 新密码
        /// </summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [MinLength(6, ErrorMessage = "密码长度不能少于6个字符")]
        public string NewPassword { get; set; } = string.Empty;
        
        /// <summary>
        /// 确认新密码
        /// </summary>
        [Required(ErrorMessage = "请确认新密码")]
        [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 修改密码命令处理器
    /// </summary>
    public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, BaseResponse>
    {
        private readonly IAuthDomainService _authDomainService;
        private readonly IUnitOfWork _unitOfWork;
        
        public ChangePasswordCommandHandler(
            IAuthDomainService authDomainService,
            IUnitOfWork unitOfWork)
        {
            _authDomainService = authDomainService ?? throw new ArgumentNullException(nameof(authDomainService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        
        public async Task<BaseResponse> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                // 验证旧密码
                var isValid = await _authDomainService.ValidateUserPasswordAsync(command.UserId, command.OldPassword);
                if (!isValid)
                {
                    return BaseResponse.Fail("旧密码不正确");
                }
                
                // 修改密码，添加缺少的新密码参数
                await _authDomainService.ChangePasswordAsync(command.UserId, command.OldPassword, command.NewPassword);
                
                // 保存更改
                await _unitOfWork.SaveChangesAsync();
                
                return BaseResponse.Ok("密码修改成功");
            }
            catch (Exception ex)
            {
                return BaseResponse.Fail($"修改密码失败: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 重置密码命令
    /// </summary>
    public class ResetPasswordCommand : ICommand<BaseResponse>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// 新密码
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
        
        public ResetPasswordCommand(ResetPasswordRequest request)
        {
            UserId = request.UserId;
            NewPassword = request.NewPassword;
        }
    }
    
    /// <summary>
    /// 重置密码命令处理器
    /// </summary>
    public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, BaseResponse>
    {
        private readonly IAuthDomainService _authDomainService;
        private readonly IUnitOfWork _unitOfWork;
        
        public ResetPasswordCommandHandler(
            IAuthDomainService authDomainService,
            IUnitOfWork unitOfWork)
        {
            _authDomainService = authDomainService ?? throw new ArgumentNullException(nameof(authDomainService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        
        public async Task<BaseResponse> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                // 重置密码
                await _authDomainService.ResetPasswordAsync(command.UserId, command.NewPassword);
                
                // 保存更改
                await _unitOfWork.SaveChangesAsync();
                
                return BaseResponse.Ok("密码重置成功");
            }
            catch (Exception ex)
            {
                return BaseResponse.Fail($"重置密码失败: {ex.Message}");
            }
        }
    }
}