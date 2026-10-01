using System;
using System.Collections.Generic;

namespace PersonalFinance.API.Models
{
    public class Wallet
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MonthlyBudget { get; set; } = 0m; // Ngân sách chi tiêu trong tháng
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property 1 - N
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
