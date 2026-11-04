using PersonalFinance.API.Security;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;
using PersonalFinance.API.Models;

namespace PersonalFinance.API.Controllers
{
    public record DebtPaymentRequest(Guid Id, Guid WalletId, decimal Amount);

    public class SaveDebtRequest
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? RecordType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? BreakdownNote { get; set; }
        public DateTime? DueDate { get; set; }
        public string? PeriodLabel { get; set; }
        public int PriorityLevel { get; set; } = 2;
        public bool IsCompleted { get; set; }
        public string? CategoryTag { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DebtsController : OwnedController
    {
        private readonly AppDbContext _context;

        public DebtsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DebtRecord>>> GetDebts()
        {

            var query = _context.DebtRecords.AsNoTracking().AsQueryable();
            if (userId.HasValue)
            {
                query = query.Where(d => d.UserId == userId.Value);
            }

            var records = await query
                .OrderBy(d => d.IsCompleted)
                .ThenBy(d => d.PriorityLevel)
                .ThenBy(d => d.DueDate)
                .ToListAsync();

            return Ok(records);
        }

        [HttpPost]
        public async Task<ActionResult<DebtRecord>> CreateDebt(
            [FromBody] SaveDebtRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200 || request.TotalAmount < 0 || request.PaidAmount < 0 || request.PaidAmount > request.TotalAmount || !new[] { "DEBT", "INCOME", "BORROW_MORE" }.Contains(request.RecordType?.ToUpperInvariant() ?? "DEBT")) return BadRequest(new { message = "Kiểm tra tên, số tiền và loại khoản mục." });
            Guid recordId = Guid.NewGuid();
            if (!string.IsNullOrWhiteSpace(request.Id) && Guid.TryParse(request.Id, out var parsedGuid) && parsedGuid != Guid.Empty)
            {
                // Nếu đã tồn tại bản ghi có cùng Guid trên server thì cập nhật thay vì báo lỗi trùng khóa
                var existing = await _context.DebtRecords.FirstOrDefaultAsync(x => x.Id == parsedGuid && x.UserId == userId);
                if (existing != null)
                {
                    existing.UserId = userId ?? existing.UserId;
                    existing.Title = request.Title?.Trim() ?? existing.Title;
                    existing.RecordType = string.IsNullOrWhiteSpace(request.RecordType) ? existing.RecordType : request.RecordType.Trim().ToUpper();
                    existing.TotalAmount = Math.Max(0, request.TotalAmount);
                    existing.PaidAmount = Math.Max(0, request.PaidAmount);
                    existing.BreakdownNote = request.BreakdownNote?.Trim();
                    if (request.DueDate.HasValue && request.DueDate.Value != default) existing.DueDate = request.DueDate.Value;
                    existing.PeriodLabel = string.IsNullOrWhiteSpace(request.PeriodLabel) ? existing.PeriodLabel : request.PeriodLabel.Trim();
                    existing.PriorityLevel = request.PriorityLevel is >= 1 and <= 3 ? request.PriorityLevel : existing.PriorityLevel;
                    existing.IsCompleted = request.IsCompleted;
                    existing.CategoryTag = request.CategoryTag?.Trim();
                    await _context.SaveChangesAsync();
                    return Ok(existing);
                }
                recordId = parsedGuid;
            }

            var record = new DebtRecord
            {
                Id = recordId,
                UserId = userId,
                Title = request.Title?.Trim() ?? "Khoản mục mới",
                RecordType = string.IsNullOrWhiteSpace(request.RecordType) ? "DEBT" : request.RecordType.Trim().ToUpper(),
                TotalAmount = Math.Max(0, request.TotalAmount),
                PaidAmount = Math.Max(0, request.PaidAmount),
                BreakdownNote = request.BreakdownNote?.Trim(),
                DueDate = request.DueDate.HasValue && request.DueDate.Value != default ? request.DueDate.Value : DateTime.UtcNow,
                PeriodLabel = string.IsNullOrWhiteSpace(request.PeriodLabel) ? "1-10" : request.PeriodLabel.Trim(),
                PriorityLevel = request.PriorityLevel is >= 1 and <= 3 ? request.PriorityLevel : 2,
                IsCompleted = request.IsCompleted,
                CategoryTag = request.CategoryTag?.Trim() ?? "Khác",
                CreatedAt = DateTime.UtcNow
            };

            _context.DebtRecords.Add(record);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDebts), new { id = record.Id }, record);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<DebtRecord>> UpdateDebt(
            Guid id,
            [FromBody] SaveDebtRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200 || request.TotalAmount < 0 || request.PaidAmount < 0 || request.PaidAmount > request.TotalAmount || !new[] { "DEBT", "INCOME", "BORROW_MORE" }.Contains(request.RecordType?.ToUpperInvariant() ?? "DEBT")) return BadRequest(new { message = "Kiểm tra tên, số tiền và loại khoản mục." });
            var existing = await _context.DebtRecords.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (existing == null)
            {
                // Nếu chưa có trên server (do tạo lúc offline), tự động tạo mới luôn với đúng ID
                var newRecord = new DebtRecord
                {
                    Id = id,
                    UserId = userId,
                    Title = request.Title?.Trim() ?? "Khoản mục mới",
                    RecordType = string.IsNullOrWhiteSpace(request.RecordType) ? "DEBT" : request.RecordType.Trim().ToUpper(),
                    TotalAmount = Math.Max(0, request.TotalAmount),
                    PaidAmount = Math.Max(0, request.PaidAmount),
                    BreakdownNote = request.BreakdownNote?.Trim(),
                    DueDate = request.DueDate.HasValue && request.DueDate.Value != default ? request.DueDate.Value : DateTime.UtcNow,
                    PeriodLabel = string.IsNullOrWhiteSpace(request.PeriodLabel) ? "1-10" : request.PeriodLabel.Trim(),
                    PriorityLevel = request.PriorityLevel is >= 1 and <= 3 ? request.PriorityLevel : 2,
                    IsCompleted = request.IsCompleted,
                    CategoryTag = request.CategoryTag?.Trim() ?? "Khác",
                    CreatedAt = DateTime.UtcNow
                };
                _context.DebtRecords.Add(newRecord);
                await _context.SaveChangesAsync();
                return Ok(newRecord);
            }

            existing.UserId = userId ?? existing.UserId;
            existing.Title = request.Title?.Trim() ?? existing.Title;
            existing.RecordType = string.IsNullOrWhiteSpace(request.RecordType) ? existing.RecordType : request.RecordType.Trim().ToUpper();
            existing.TotalAmount = Math.Max(0, request.TotalAmount);
            existing.PaidAmount = Math.Max(0, request.PaidAmount);
            existing.BreakdownNote = request.BreakdownNote?.Trim();
            if (request.DueDate.HasValue && request.DueDate.Value != default) existing.DueDate = request.DueDate.Value;
            existing.PeriodLabel = string.IsNullOrWhiteSpace(request.PeriodLabel) ? existing.PeriodLabel : request.PeriodLabel.Trim();
            existing.PriorityLevel = request.PriorityLevel is >= 1 and <= 3 ? request.PriorityLevel : existing.PriorityLevel;
            existing.IsCompleted = request.IsCompleted;
            existing.CategoryTag = request.CategoryTag?.Trim();

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpPost("{id:guid}/payments")]
        public async Task<IActionResult> Pay(Guid id, [FromBody] DebtPaymentRequest request) {
            if (request.Amount <= 0 || request.Id == Guid.Empty) return BadRequest(new { message = "Số tiền và mã thanh toán không hợp lệ." });
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () => {
                await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var debt = await _context.DebtRecords.FirstOrDefaultAsync(x => x.Id == id && x.RecordType == "DEBT");
                var wallet = await _context.Wallets.FirstOrDefaultAsync(x => x.Id == request.WalletId);
                if (debt == null || wallet == null) return NotFound();
                if (await _context.Transactions.AnyAsync(x => x.Id == request.Id)) return Ok(debt);
                if (request.Amount > debt.TotalAmount - debt.PaidAmount) return BadRequest(new { message = "Số tiền vượt khoản nợ còn lại." });
                var balance = await _context.Transactions.Where(x => x.WalletId == wallet.Id).SumAsync(x => (decimal?)x.Amount) ?? 0;
                if (request.Amount > balance) return BadRequest(new { message = "Số dư ví không đủ." });
                _context.Transactions.Add(new Transaction { Id = request.Id, WalletId = wallet.Id, DebtRecordId = debt.Id, Amount = -request.Amount, Category = "Trả nợ", Note = debt.Title });
                debt.PaidAmount += request.Amount;
                debt.IsCompleted = debt.PaidAmount >= debt.TotalAmount;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(debt);
            });
        }

        [HttpPatch("{id:guid}/toggle")]
        public async Task<ActionResult<DebtRecord>> ToggleDebtCompleted(Guid id)
        {

            var existing = await _context.DebtRecords.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy khoản mục." });

            existing.IsCompleted = !existing.IsCompleted;
            if (existing.IsCompleted)
            {
                existing.PaidAmount = existing.TotalAmount;
            }
            else
            {
                existing.PaidAmount = 0m;
            }

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDebt(Guid id)
        {

            var existing = await _context.DebtRecords.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy khoản mục." });

            _context.DebtRecords.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
