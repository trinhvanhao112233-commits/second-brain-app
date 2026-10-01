using System;

namespace PersonalFinance.API.Models
{
    public class CalendarEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Category { get; set; } = "Personal"; // Work, Personal, Health, Important
        public string Color { get; set; } = "#10b981"; // HEX color code
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
