using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonalFinance.API.Data;

namespace PersonalFinance.API.Services
{
    public class EventReminderBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventReminderBackgroundWorker> _logger;

        public EventReminderBackgroundWorker(IServiceProvider serviceProvider, ILogger<EventReminderBackgroundWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[EventReminderBackgroundWorker] Đã khởi động dịch vụ quét nhắc nhở lịch trình trước 1 tiếng.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EventReminderBackgroundWorker] Lỗi trong chu kỳ quét nhắc nhở.");
                }

                // Quét mỗi 1 phút một lần
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CheckAndSendRemindersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // Lấy tất cả sự kiện chưa hoàn thành và chưa gửi thông báo
            var pendingEvents = await db.Events
                .Where(e => !e.IsCompleted && !e.IsReminderSent)
                .ToListAsync();

            if (pendingEvents.Count == 0) return;

            var utcNow = DateTime.UtcNow;
            var vnNow = DateTime.UtcNow.AddHours(7); // Giờ chuẩn Việt Nam (GMT+7)

            foreach (var ev in pendingEvents)
            {
                if (!ev.UserId.HasValue) continue;

                // Xác định thời gian bắt đầu của sự kiện
                // Nếu StartTime lưu dạng giờ địa phương (15:25), so sánh trực tiếp với vnNow.
                // Nếu StartTime lưu dạng UTC (08:25), chênh lệch vẫn tính được chuẩn xác.
                double minutesLeft = (ev.StartTime - vnNow).TotalMinutes;
                
                // Trường hợp nếu StartTime đã được lưu dưới dạng UTC
                if (minutesLeft < -120 || minutesLeft > 1440)
                {
                    var altMinutes = (ev.StartTime - utcNow).TotalMinutes;
                    if (Math.Abs(altMinutes) < Math.Abs(minutesLeft))
                    {
                        minutesLeft = altMinutes;
                    }
                }

                // Nếu sự kiện sắp diễn ra trong khoảng từ 0 đến 65 phút tới (ví dụ: trước 1 tiếng, trước 30p, hoặc vừa tạo lịch cận giờ)
                if (minutesLeft >= 0 && minutesLeft <= 65)
                {
                    var userSubs = await db.PushSubscriptions
                        .Where(s => s.UserId == ev.UserId.Value)
                        .ToListAsync();

                    var displayTime = ev.StartTime.ToString("HH:mm");
                    var title = "🔔 Nhắc nhở: Lịch trình sắp bắt đầu!";
                    string timePrompt = minutesLeft > 45 
                        ? "còn khoảng 1 tiếng nữa" 
                        : (minutesLeft > 15 ? $"còn khoảng {(int)minutesLeft} phút nữa" : "chuẩn bị diễn ra ngay bây giờ");
                    var message = $"Lịch '{ev.Title}' của bạn lúc {displayTime} ({timePrompt})!";

                    foreach (var sub in userSubs)
                    {
                        try
                        {
                            await notificationService.SendPushNotificationAsync(sub, title, message, "/calendar");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"[EventReminderBackgroundWorker] Lỗi gửi push cho user {ev.UserId}: {ex.Message}");
                        }
                    }

                    ev.IsReminderSent = true;
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
