using PersonalFinance.API.Security;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;
using PersonalFinance.API.Models;
using PersonalFinance.API.Services;

namespace PersonalFinance.API.Controllers
{
    public record PushSubscribeRequest(
        string Endpoint,
        PushKeys Keys
    );

    public record PushKeys(
        string P256dh,
        string Auth
    );

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : OwnedController
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly INotificationService _notificationService;

        public NotificationController(AppDbContext context, INotificationService notificationService, IConfiguration config)
        {
            _context = context;
            _config = config;
            _notificationService = notificationService;
        }

        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            var publicKey = _config["Vapid:PublicKey"] ?? Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY");
            if (string.IsNullOrWhiteSpace(publicKey)) return StatusCode(503, new { message = "Máy chủ chưa cấu hình thông báo đẩy." });
            return Ok(new { publicKey });
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] PushSubscribeRequest request) {
            var rows = await _context.PushSubscriptions.Where(x => x.Endpoint == request.Endpoint).ToListAsync();
            _context.PushSubscriptions.RemoveRange(rows);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences() {
            var user = await _context.Users.FindAsync(userId!.Value);
            return Ok(string.IsNullOrEmpty(user?.NotificationSettings) ? new NotificationPreferences() : System.Text.Json.JsonSerializer.Deserialize<NotificationPreferences>(user.NotificationSettings));
        }
        [HttpPut("preferences")]
        public async Task<IActionResult> SetPreferences([FromBody] NotificationPreferences preferences) {
            if (preferences.QuietStart is < 0 or > 23 || preferences.QuietEnd is < 0 or > 23) return BadRequest();
            var user = await _context.Users.FindAsync(userId!.Value);
            if (user == null) return Unauthorized();
            user.NotificationSettings = System.Text.Json.JsonSerializer.Serialize(preferences);
            await _context.SaveChangesAsync();
            return Ok(preferences);
        }
        [HttpGet("devices")]
        public async Task<IActionResult> Devices() => Ok(await _context.PushSubscriptions.Select(x => new { x.Id, x.CreatedAt }).ToListAsync());

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe(
            [FromBody] PushSubscribeRequest request)
        {
            var allowedHosts = _config.GetSection("Push:AllowedHosts").Get<string[]>() ?? new[] { "fcm.googleapis.com", "updates.push.services.mozilla.com", "push.apple.com", "notify.windows.com" };
            if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https" || !string.IsNullOrEmpty(endpoint.UserInfo)
                || !allowedHosts.Any(host => endpoint.Host.Equals(host, StringComparison.OrdinalIgnoreCase) || endpoint.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase)))
                return BadRequest(new { message = "Địa chỉ dịch vụ Push không được hỗ trợ." });
            if (string.IsNullOrWhiteSpace(request.Endpoint) || request.Keys == null)
            {
                return BadRequest(new { message = "Thông tin đăng ký Push không hợp lệ." });
            }

            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);

            if (existing != null)
            {
                existing.UserId = userId ?? existing.UserId;
                existing.P256dh = request.Keys.P256dh;
                existing.Auth = request.Keys.Auth;
            }
            else
            {
                var newSub = new PushSubscriptionModel
                {
                    UserId = userId,
                    Endpoint = request.Endpoint,
                    P256dh = request.Keys.P256dh,
                    Auth = request.Keys.Auth,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PushSubscriptions.Add(newSub);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã lưu đăng ký nhận thông báo đẩy thành công!" });
        }

        [HttpPost("test-push")]
        public async Task<IActionResult> TestPush()
        {
            if (!userId.HasValue) return BadRequest(new { message = "Cần đăng nhập để test thông báo." });

            var subs = await _context.PushSubscriptions
                .Where(s => s.UserId == userId.Value)
                .ToListAsync();

            if (subs.Count == 0)
            {
                return BadRequest(new { message = "Chưa tìm thấy thiết bị nào đăng ký nhận thông báo cho tài khoản này." });
            }

            var sent = 0;
            foreach (var sub in subs)
            {
                if (await _notificationService.SendPushNotificationAsync(
                    sub, 
                    "🔔 Thử nghiệm thông báo thành công!", 
                    "Hệ thống Second Brain đã kết nối thông báo đẩy trên trình duyệt của bạn!"
                )) sent++;
            }

            return Ok(new { sent, total = subs.Count, message = $"Đã gửi thành công đến {sent}/{subs.Count} thiết bị." });
        }
    }
}
