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
    [ApiController]
    [Route("api/[controller]")]
    public class DebtsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DebtsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DebtRecord>>> GetDebts([FromHeader(Name = "X-User-Id")] Guid? userId)
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
            [FromHeader(Name = "X-User-Id")] Guid? userId,
            [FromBody] DebtRecord request)
        {
            var record = new DebtRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title?.Trim() ?? "Khoản mục mới",
                RecordType = string.IsNullOrWhiteSpace(request.RecordType) ? "DEBT" : request.RecordType.Trim().ToUpper(),
                TotalAmount = Math.Max(0, request.TotalAmount),
                PaidAmount = Math.Max(0, request.PaidAmount),
                BreakdownNote = request.BreakdownNote?.Trim(),
                DueDate = request.DueDate == default ? DateTime.UtcNow : request.DueDate,
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
            [FromBody] DebtRecord request)
        {
            var existing = await _context.DebtRecords.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy khoản mục." });

            existing.Title = request.Title?.Trim() ?? existing.Title;
            existing.RecordType = string.IsNullOrWhiteSpace(request.RecordType) ? existing.RecordType : request.RecordType.Trim().ToUpper();
            existing.TotalAmount = Math.Max(0, request.TotalAmount);
            existing.PaidAmount = Math.Max(0, request.PaidAmount);
            existing.BreakdownNote = request.BreakdownNote?.Trim();
            if (request.DueDate != default) existing.DueDate = request.DueDate;
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
            var existing = await _context.DebtRecords.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy khoản mục." });

            _context.DebtRecords.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// POST /api/debts/seed-sample
        /// Nạp nhanh dữ liệu mẫu từ ảnh của người dùng (Shopee easy, Shopee spay, Giang, MoMo, Tiền học, Đi làm...)
        /// </summary>
        [HttpPost("seed-sample")]
        public async Task<ActionResult<IEnumerable<DebtRecord>>> SeedSampleData([FromHeader(Name = "X-User-Id")] Guid? userId)
        {
            var now = DateTime.UtcNow;
            var y = now.Year;
            var m = now.Month;

            var sampleItems = new List<DebtRecord>
            {
                // MỐC 1-10: TIỀN VÀO (2000 + 4300 = 6300)
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Lương Đi Làm",
                    RecordType = "INCOME",
                    TotalAmount = 4300000,
                    BreakdownNote = "4300k (Lương về đầu tháng)",
                    DueDate = new DateTime(y, m, 5, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "1-10",
                    PriorityLevel = 1,
                    CategoryTag = "Lương / Thu nhập"
                },
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Tiền Ăn (Đợt 1)",
                    RecordType = "INCOME",
                    TotalAmount = 2000000,
                    BreakdownNote = "2000k (Trong tổng 4000k tiền ăn)",
                    DueDate = new DateTime(y, m, 2, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "1-10",
                    PriorityLevel = 1,
                    CategoryTag = "Trợ cấp / Tiền ăn"
                },

                // MỐC 1-10: TIỀN RA / NỢ PHẢI TRẢ (1850 + 600 + 811 + 1000(MOMO) = 4261)
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Nợ Shopee Easy (Đợt mùng 5-10)",
                    RecordType = "DEBT",
                    TotalAmount = 2450000,
                    BreakdownNote = "1850 + 600 = 2450k (Tổng Shopee Easy: 3375k)",
                    DueDate = new DateTime(y, m, 8, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "1-10",
                    PriorityLevel = 1,
                    CategoryTag = "Ví trả sau / App"
                },
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Nợ Shopee SPayLater",
                    RecordType = "DEBT",
                    TotalAmount = 811000,
                    BreakdownNote = "811k (Hạn mùng 10 hàng tháng)",
                    DueDate = new DateTime(y, m, 10, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "1-10",
                    PriorityLevel = 1,
                    CategoryTag = "Ví trả sau / App"
                },
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Trả góp Nợ MoMo (Gốc 10tr)",
                    RecordType = "DEBT",
                    TotalAmount = 1000000,
                    BreakdownNote = "1000k (Trích trả 1tr trong tổng nợ MoMo 10tr)",
                    DueDate = new DateTime(y, m, 9, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "1-10",
                    PriorityLevel = 1,
                    CategoryTag = "Ví trả sau / App"
                },

                // MỐC 10-21: TIỀN VÀO (3600 + 2000 = 5600)
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Tiền Học",
                    RecordType = "INCOME",
                    TotalAmount = 3600000,
                    BreakdownNote = "3600k",
                    DueDate = new DateTime(y, m, 12, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "10-21",
                    PriorityLevel = 2,
                    CategoryTag = "Tiền học"
                },
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Tiền Ăn (Đợt 2)",
                    RecordType = "INCOME",
                    TotalAmount = 2000000,
                    BreakdownNote = "2000k (Đợt 2 giữa tháng)",
                    DueDate = new DateTime(y, m, 15, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "10-21",
                    PriorityLevel = 2,
                    CategoryTag = "Trợ cấp / Tiền ăn"
                },

                // MỐC 10-21: TIỀN RA / NỢ PHẢI TRẢ (1234 + 370 + 555 = 2159)
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Nợ Shopee Easy (Đợt 20)",
                    RecordType = "DEBT",
                    TotalAmount = 925000,
                    BreakdownNote = "370 + 555 = 925k (Hoàn tất 3375k Shopee Easy)",
                    DueDate = new DateTime(y, m, 18, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "10-21",
                    PriorityLevel = 1,
                    CategoryTag = "Ví trả sau / App"
                },
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Nợ Giang",
                    RecordType = "DEBT",
                    TotalAmount = 1234000,
                    BreakdownNote = "1234k (Trả nợ cá nhân)",
                    DueDate = new DateTime(y, m, 20, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "10-21",
                    PriorityLevel = 2,
                    CategoryTag = "Nợ cá nhân / Bạn bè"
                },

                // KHOẢN NỢ TỔNG DÀI HẠN GHI NHỚ (Phần còn lại của MoMo 10tr)
                new DebtRecord
                {
                    UserId = userId,
                    Title = "Dư nợ gốc MoMo còn lại",
                    RecordType = "DEBT",
                    TotalAmount = 9000000,
                    BreakdownNote = "Tổng nợ MoMo 10tr (Đã lên lịch trả 1tr đợt 1-10, còn 9tr)",
                    DueDate = new DateTime(y, m, 28, 0, 0, 0, DateTimeKind.Utc),
                    PeriodLabel = "21-31",
                    PriorityLevel = 3,
                    CategoryTag = "Dư nợ dài hạn"
                }
            };

            _context.DebtRecords.AddRange(sampleItems);
            await _context.SaveChangesAsync();

            return Ok(sampleItems);
        }
    }
}

