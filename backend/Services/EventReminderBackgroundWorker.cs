using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;

namespace PersonalFinance.API.Services;

public class EventReminderBackgroundWorker(IServiceProvider services, ILogger<EventReminderBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do {
            try { await Check(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError(error, "Không quét được nhắc nhở."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task Check(CancellationToken token)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ReminderDispatcher>();
        // Calendar API stores wall-clock time in Asia/Bangkok (UTC+7), not server-local time.
        var now = DateTime.UtcNow.AddHours(7);
        var events = await db.Events.IgnoreQueryFilters()
            .Where(x => !x.IsCompleted && x.UserId != null && x.StartTime >= now && x.StartTime <= now.AddHours(1)).ToListAsync(token);
        foreach (var row in events)
            await dispatcher.Send(row.UserId!.Value, $"event:{row.Id}:{row.StartTime:O}", "Lịch sắp bắt đầu", $"{row.Title} · {row.StartTime:HH:mm dd/MM}", "/?tab=calendar");
        var tasks = await db.Tasks.IgnoreQueryFilters()
            .Where(x => !x.IsCompleted && x.UserId != null && x.DueDate != null && x.DueDate < now.Date.AddDays(2)).ToListAsync(token);
        foreach (var row in tasks)
            await dispatcher.Send(row.UserId!.Value, $"task:{row.Id}:{row.DueDate:yyyyMMdd}:{now:yyyyMMdd}", "Công việc cần chú ý", $"{row.Title} · hạn {row.DueDate:dd/MM}", "/?tab=tasks");
        var debts = await db.DebtRecords.IgnoreQueryFilters()
            .Where(x => !x.IsCompleted && x.RecordType == "DEBT" && x.UserId != null && x.DueDate < now.Date.AddDays(4)).ToListAsync(token);
        foreach (var row in debts)
            await dispatcher.Send(row.UserId!.Value, $"debt:{row.Id}:{row.DueDate:yyyyMMdd}:{now:yyyyMMdd}", "Khoản nợ cần chú ý", $"{row.Title} · hạn {row.DueDate:dd/MM}", "/?tab=debts");
    }
}
