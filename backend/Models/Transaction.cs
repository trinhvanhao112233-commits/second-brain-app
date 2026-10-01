using System;

namespace PersonalFinance.API.Models
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; } // Số dương: Nạp / Thu nhập, Số âm: Chi tiêu
        public string Category { get; set; } = "Khác"; // Ăn uống, Đi lại, Mua sắm, Hóa đơn, Giải trí...
        public string? Note { get; set; } // Ghi chú chi tiêu
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow; // Ngày giao dịch
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property N - 1
        public Wallet? Wallet { get; set; }
    }
}
