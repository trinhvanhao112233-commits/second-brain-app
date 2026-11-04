using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;
using PersonalFinance.API.Models;

namespace PersonalFinance.API.Services;

public sealed class ReminderDispatcher(AppDbContext db, INotificationService push)
{
    public async Task Send(Guid userId, string key, string title, string message, string url)
    {
        var user = await db.Users.FindAsync(userId);
        var preferences = string.IsNullOrEmpty(user?.NotificationSettings) ? new NotificationPreferences()
            : System.Text.Json.JsonSerializer.Deserialize<NotificationPreferences>(user.NotificationSettings) ?? new();
        if ((key.StartsWith("event:") && !preferences.Events) || (key.StartsWith("task:") && !preferences.Tasks) || (key.StartsWith("debt:") && !preferences.Debts)) return;
        var hour = DateTime.UtcNow.AddHours(7).Hour;
        if (preferences.QuietEnabled && (preferences.QuietStart == preferences.QuietEnd ||
            (preferences.QuietStart < preferences.QuietEnd ? hour >= preferences.QuietStart && hour < preferences.QuietEnd : hour >= preferences.QuietStart || hour < preferences.QuietEnd))) return;
        var subscriptions = await db.PushSubscriptions.IgnoreQueryFilters().Where(x => x.UserId == userId).ToListAsync();
        foreach (var sub in subscriptions)
        {
            var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{key}:{sub.Id}")));
            var delivery = await db.ReminderDeliveries.FindAsync(id);
            if (delivery?.Sent == true || delivery?.AttemptedAt > DateTime.UtcNow.AddMinutes(-5)) continue;
            if (delivery == null) {
                delivery = new ReminderDelivery { Id = id, UserId = userId, SubscriptionId = sub.Id };
                db.ReminderDeliveries.Add(delivery);
            }
            delivery.AttemptedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            delivery.Sent = await push.SendPushNotificationAsync(sub, title, message, url);
            await db.SaveChangesAsync();
        }
    }
}
