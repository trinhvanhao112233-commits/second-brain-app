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

    [ApiController]
    [Route("api/[controller]")]
    public class DebtsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private static bool _tableEnsured = false;

        public DebtsController(AppDbContext context)
        {
            _context = context;
        }

        private async Task EnsureTableAsync()
        {
            if (_tableEnsured) return;
            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS `DebtRecords` (
                        `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
                        `UserId` char(36) COLLATE utf8mb4_general_ci NULL,
                        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                        `RecordType` varchar(30) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'DEBT',
                        `TotalAmount` decimal(18,2) NOT NULL DEFAULT 0,
                        `PaidAmount` decimal(18,2) NOT NULL DEFAULT 0,
                        `BreakdownNote` varchar(500) CHARACTER SET utf8mb4 NULL,
                        `DueDate` datetime(6) NOT NULL,
                        `PeriodLabel` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT '1-10',
                        `PriorityLevel` int NOT NULL DEFAULT 2,
                        `IsCompleted` tinyint(1) NOT NULL DEFAULT 0,
                        `CategoryTag` varchar(100) CHARACTER SET utf8mb4 NULL,
                        `CreatedAt` datetime(6) NOT NULL,
                        PRIMARY KEY (`Id`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                ");
                _tableEnsured = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EnsureTableAsync Warning]: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DebtRecord>>> GetDebts([FromHeader(Name = "X-User-Id")] Guid? userId)
        {
            await EnsureTableAsync();

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
            [FromHeader(Name = "X-User-Id")] Guid? userId,
            [FromBody] SaveDebtRequest request)
        {
            await EnsureTableAsync();

            Guid recordId = Guid.NewGuid();
            if (!string.IsNullOrWhiteSpace(request.Id) && Guid.TryParse(request.Id, out var parsedGuid) && parsedGuid != Guid.Empty)
            {
                // Nếu đã tồn tại bản ghi có cùng Guid trên server thì cập nhật thay vì báo lỗi trùng khóa
                var existing = await _context.DebtRecords.FindAsync(parsedGuid);
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
            [FromHeader(Name = "X-User-Id")] Guid? userId,
            [FromBody] SaveDebtRequest request)
        {
            await EnsureTableAsync();

            var existing = await _context.DebtRecords.FindAsync(id);
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

        [HttpPatch("{id:guid}/toggle")]
        public async Task<ActionResult<DebtRecord>> ToggleDebtCompleted(Guid id)
        {
            await EnsureTableAsync();

            var existing = await _context.DebtRecords.FindAsync(id);
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
            await EnsureTableAsync();

            var existing = await _context.DebtRecords.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy khoản mục." });

            _context.DebtRecords.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
