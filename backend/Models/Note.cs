using System;

namespace PersonalFinance.API.Models
{
    public class Note
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Color { get; set; } = "#3b82f6"; // Blue, Amber, Rose, Emerald...
        public bool IsPinned { get; set; } = false;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
