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

            var now = DateTime.UtcNow;
            // Tìm sự kiện diễn ra trong vòng 50 - 65 phút tới mà chưa gửi nhắc nhở
            var lowerWindow = now.AddMinutes(50);
            var upperWindow = now.AddMinutes(65);

            var upcomingEvents = await db.Events
                .Where(e => !e.IsCompleted && !e.IsReminderSent && e.StartTime >= lowerWindow && e.StartTime <= upperWindow)
                .ToListAsync();

            if (upcomingEvents.Count == 0) return;

            foreach (var ev in upcomingEvents)
            {
                if (!ev.UserId.HasValue) continue;

                // Lấy danh sách đăng ký Push của User này
                var userSubs = await db.PushSubscriptions
                    .Where(s => s.UserId == ev.UserId.Value)
                    .ToListAsync();

                var localTime = ev.StartTime.ToLocalTime();
                var title = "🔔 Nhắc nhở lịch trình sắp diễn ra!";
                var message = $"Lịch '{ev.Title}' của bạn sẽ bắt đầu lúc {localTime:HH:mm} (còn khoảng 1 tiếng nữa)!";

                foreach (var sub in userSubs)
                {
                    await notificationService.SendPushNotificationAsync(sub, title, message, "/calendar");
                }

                ev.IsReminderSent = true;
            }

            await db.SaveChangesAsync();
        }
    }
}
