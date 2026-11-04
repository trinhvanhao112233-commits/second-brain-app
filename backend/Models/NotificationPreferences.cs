namespace PersonalFinance.API.Models;

public sealed record NotificationPreferences(
    bool Events = true, bool Tasks = true, bool Debts = true,
    bool QuietEnabled = false, int QuietStart = 22, int QuietEnd = 7);
