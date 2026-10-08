using System;
using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.API.DTOs
{
    // === WALLETS ===
    public record CreateWalletRequest(
        [Required(ErrorMessage = "Tên ví không được để trống")]
        [StringLength(100, MinimumLength = 1)]
        string Name,
        decimal MonthlyBudget = 0m,
        bool IsMaster = false
    );

    public record UpdateBudgetRequest(
        [Range(0, double.MaxValue, ErrorMessage = "Ngân sách phải lớn hơn hoặc bằng 0")]
        decimal MonthlyBudget
    );

    public record SetMasterWalletRequest(
        bool IsMaster
    );

    public record TransferWalletRequest(
        [Required]
        Guid SourceWalletId,
        [Required]
        Guid TargetWalletId,
        [Range(1, double.MaxValue, ErrorMessage = "Số tiền chuyển phải lớn hơn 0")]
        decimal Amount,
        string? Note,
        DateTime? TransactionDate
    );

    public record WalletResponse(
        Guid Id,
        string Name,
        decimal Balance,
        decimal MonthlyBudget,
        decimal MonthlySpent,
        bool IsMaster,
        DateTime CreatedAt
    );

    public record CreateTransactionRequest(
        [Required]
        decimal Amount,
        string? Category,
        string? Note,
        DateTime? TransactionDate,
        bool DeductFromMaster = false // Nếu nạp tiền vào ví con, có thể trừ tiền tương ứng từ Ví Tổng
    );

    public record TransactionResponse(
        Guid Id,
        Guid WalletId,
        decimal Amount,
        string Category,
        string? Note,
        DateTime TransactionDate,
        decimal NewBalance,
        DateTime CreatedAt
    );

    // === CALENDAR EVENTS ===
    public record CreateEventRequest(
        [Required] string Title,
        string? Description,
        DateTime StartTime,
        DateTime EndTime,
        string Category,
        string Color
    );

    public record UpdateEventRequest(
        [Required] string Title,
        string? Description,
        DateTime StartTime,
        DateTime EndTime,
        string Category,
        string Color,
        bool IsCompleted
    );

    // === DAILY TASKS ===
    public record CreateTaskRequest(
        [Required] string Title,
        DateTime? DueDate,
        string Priority
    );

    public record UpdateTaskRequest(
        [Required] string Title,
        DateTime? DueDate,
        string Priority,
        bool IsCompleted
    );

    // === NOTES ===
    public record CreateNoteRequest(
        [Required] string Title,
        string Content,
        string Color,
        bool IsPinned
    );

    public record UpdateNoteRequest(
        [Required] string Title,
        string Content,
        string Color,
        bool IsPinned
    );
}
