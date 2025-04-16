using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserPermissionSystem.DTOs;
using UserPermissionSystem.Services;

namespace UserPermissionSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result.Data);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "无效的用户身份" });
                }

                var userInfo = await _authService.GetUserInfoAsync(userId);
                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPatch("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "无效的用户身份" });
            }

            var result = await _authService.ChangePasswordAsync(userId, request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
        
        // 添加系统自检接口，用于验证密码哈希
        [HttpGet("system-check")]
        public IActionResult SystemCheck()
        {
            // 验证预设的admin123密码哈希是否一致
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var password = "admin123";
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var hash = Convert.ToBase64String(hashedBytes);
                var expectedHash = "qnR8UCqJggD55PohusaBNviGoOJ67HC6Btry4qXLVZc=";
                
                var isMatching = hash == expectedHash;
                
                return Ok(new { 
                    password = password,
                    generatedHash = hash,
                    expectedHash = expectedHash,
                    isMatching = isMatching,
                    message = isMatching ? "哈希值匹配，系统配置正确" : "哈希值不匹配，请修复密码验证逻辑"
                });
            }
        }
    }
}