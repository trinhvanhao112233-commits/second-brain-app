using System;

namespace PersonalFinance.API.Models
{
    public class DebtRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public string Title { get; set; } = string.Empty; // Ví dụ: Nợ Shopee easy, Nợ Giang, Đi làm, Tiền học...
        public string RecordType { get; set; } = "DEBT";  // "DEBT" (Khoản nợ phải trả) | "INCOME" (Khoản thu/tiền vào dự kiến) | "BORROW_MORE" (Khoản vay thêm ở ngoài)
        public decimal TotalAmount { get; set; }          // Tổng số tiền (VD: 1850000)
        public decimal PaidAmount { get; set; } = 0m;     // Đã trả hoặc đã nhận
        public string? BreakdownNote { get; set; }        // Biểu thức chi tiết (VD: "1850 + 600 + 370 + 555")
        public DateTime DueDate { get; set; } = DateTime.UtcNow; // Ngày đến hạn trả hoặc ngày dự kiến tiền về
        public string PeriodLabel { get; set; } = "1-10"; // Mốc thời gian trong tháng (VD: "1-10", "10-21", "21-31")
        public int PriorityLevel { get; set; } = 2;       // 1: Gấp/Bắt buộc (Hạn cứng, lãi phạt cao), 2: Quan trọng, 3: Linh hoạt/Người quen
        public bool IsCompleted { get; set; } = false;    // Đã thanh toán xong / Đã nhận tiền
        public string? CategoryTag { get; set; }          // Ví điện tử (Shopee/MoMo), Cá nhân (Giang), Lương, Tiền học...
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

