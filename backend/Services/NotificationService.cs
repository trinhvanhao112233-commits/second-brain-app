using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonalFinance.API.Models;
using WebPush;

namespace PersonalFinance.API.Services
{
    public interface INotificationService
    {
        Task SendPushNotificationAsync(PushSubscriptionModel sub, string title, string message, string url = "/calendar");
    }

    public class WebPushNotificationService : INotificationService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<WebPushNotificationService> _logger;

        public WebPushNotificationService(IConfiguration config, ILogger<WebPushNotificationService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendPushNotificationAsync(PushSubscriptionModel sub, string title, string message, string url = "/calendar")
        {
            var publicKey = _config["Vapid:PublicKey"] 
                            ?? Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY")
                            ?? "BOOl107oDcMlc2_3Uj5hQ63qFVi2vRw0SEpborF1EZGfPAOtnwizjdckuhGylEoPjT_w4_uI50Qy3PWAS37PliI";
            var privateKey = _config["Vapid:PrivateKey"] 
                             ?? Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY")
                             ?? "T7cbMF0DeHMf7_4cHdga_A_7eOZ9BQsOrPoRPRQe9lk";
            var subject = _config["Vapid:Subject"] 
                          ?? Environment.GetEnvironmentVariable("VAPID_SUBJECT")
                          ?? "mailto:support@secondbrain.local";

            try
            {
                var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                var vapidDetails = new VapidDetails(subject, publicKey, privateKey);

                var payload = JsonSerializer.Serialize(new
                {
                    title,
                    body = message,
                    icon = "/favicon.ico",
                    data = new { url }
                });

                var webPushClient = new WebPushClient();
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                _logger.LogInformation($"[WebPush] Đã gửi thông báo thành công đến user: {sub.UserId}");
            }
            catch (WebPushException ex)
            {
                _logger.LogWarning($"[WebPush] Lỗi gửi push (StatusCode: {ex.StatusCode}): {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebPush] Ngoại lệ khi gửi push notification.");
            }
        }
    }
}
