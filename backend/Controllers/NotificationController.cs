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

    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public NotificationController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            var publicKey = Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY")
                            ?? "BOOl107oDcMlc2_3Uj5hQ63qFVi2vRw0SEpborF1EZGfPAOtnwizjdckuhGylEoPjT_w4_uI50Qy3PWAS37PliI";
            return Ok(new { publicKey });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe(
            [FromHeader(Name = "X-User-Id")] Guid? userId,
            [FromBody] PushSubscribeRequest request)
        {
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
        public async Task<IActionResult> TestPush([FromHeader(Name = "X-User-Id")] Guid? userId)
        {
            if (!userId.HasValue) return BadRequest(new { message = "Cần đăng nhập để test thông báo." });

            var subs = await _context.PushSubscriptions
                .Where(s => s.UserId == userId.Value)
                .ToListAsync();

            if (subs.Count == 0)
            {
                return BadRequest(new { message = "Chưa tìm thấy thiết bị nào đăng ký nhận thông báo cho tài khoản này." });
            }

            foreach (var sub in subs)
            {
                await _notificationService.SendPushNotificationAsync(
                    sub, 
                    "🔔 Thử nghiệm thông báo thành công!", 
                    "Hệ thống Second Brain đã kết nối thông báo đẩy trên trình duyệt của bạn!"
                );
            }

            return Ok(new { message = $"Đã gửi thử nghiệm thông báo đến {subs.Count} thiết bị của bạn." });
        }
    }
}
