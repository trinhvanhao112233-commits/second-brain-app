using Microsoft.AspNetCore.RateLimiting;
using PersonalFinance.API.Security;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;
using PersonalFinance.API.DTOs;
using PersonalFinance.API.Models;

namespace PersonalFinance.API.Controllers
{
    [EnableRateLimiting("sensitive")]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : OwnedController
    {
        private readonly AppDbContext _context;
        private readonly SessionTokens _tokens;

        public AuthController(AppDbContext context, SessionTokens tokens)
        {
            _context = context;
            _tokens = tokens;
        }

        /// <summary>
        /// POST /api/auth/register
        /// Đăng ký tài khoản mới
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
        {
            if (System.Text.Encoding.UTF8.GetByteCount(request.Password) > 72) return BadRequest(new { message = "Mật khẩu quá dài (tối đa 72 byte UTF-8)." });
            var username = request.Username.Trim().ToLowerInvariant();
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username))
            {
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác!" });
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User
            {
                Username = request.Username.Trim(),
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Username.Trim() : request.FullName.Trim(),
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new UserResponse(user.Id, user.Username, user.FullName, user.CreatedAt, _tokens.Issue(user)));
        }

        /// <summary>
        /// POST /api/auth/login
        /// Đăng nhập hệ thống
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<UserResponse>> Login([FromBody] LoginRequest request)
        {
            var username = request.Username.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác!" });
            }

            return Ok(new UserResponse(user.Id, user.Username, user.FullName, user.CreatedAt, _tokens.Issue(user)));
        }

        /// <summary>
        /// POST /api/auth/change-password
        /// Đổi mật khẩu tài khoản
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để đổi mật khẩu!" });
            }

            if (System.Text.Encoding.UTF8.GetByteCount(request.NewPassword) > 72) return BadRequest(new { message = "Mật khẩu quá dài (tối đa 72 byte UTF-8)." });
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng!" });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng!" });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!", token = _tokens.Issue(user) });
        }
    }
}
