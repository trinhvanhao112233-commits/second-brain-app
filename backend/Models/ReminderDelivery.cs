namespace PersonalFinance.API.Models;

public sealed class ReminderDelivery
{
    public string Id { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid SubscriptionId { get; set; }
    public bool Sent { get; set; }
    public DateTime AttemptedAt { get; set; }
}
