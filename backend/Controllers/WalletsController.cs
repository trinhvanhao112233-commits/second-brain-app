using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;
using PersonalFinance.API.DTOs;
using PersonalFinance.API.Models;

namespace PersonalFinance.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WalletsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/wallets
        /// Lấy danh sách ví kèm theo số dư, ngân sách tháng, và tổng tiền đã chi trong tháng hiện tại
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WalletResponse>>> GetWallets([FromHeader(Name = "X-User-Id")] Guid? userId)
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);

            var query = _context.Wallets.AsNoTracking().AsQueryable();
            if (userId.HasValue)
            {
                query = query.Where(w => w.UserId == userId.Value);
            }

            var wallets = await query
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new WalletResponse(
                    w.Id,
                    w.Name,
                    w.Transactions.Sum(t => (decimal?)t.Amount) ?? 0m,
                    w.MonthlyBudget,
                    // Chi tiêu trong tháng là các giao dịch Amount < 0
                    Math.Abs(w.Transactions
                        .Where(t => t.Amount < 0 && t.TransactionDate >= startOfMonth && t.TransactionDate < endOfMonth)
                        .Sum(t => (decimal?)t.Amount) ?? 0m),
                    w.CreatedAt
                ))
                .ToListAsync();

            return Ok(wallets);
        }

        /// <summary>
        /// POST /api/wallets
        /// Nhận JSON { "name": "Tên ví", "monthlyBudget": 5000000 } để tạo ví mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<WalletResponse>> CreateWallet(
            [FromHeader(Name = "X-User-Id")] Guid? userId,
            [FromBody] CreateWalletRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var wallet = new Wallet
            {
                UserId = userId,
                Name = request.Name.Trim(),
                MonthlyBudget = request.MonthlyBudget >= 0 ? request.MonthlyBudget : 0m,
                CreatedAt = DateTime.UtcNow
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            var response = new WalletResponse(wallet.Id, wallet.Name, 0m, wallet.MonthlyBudget, 0m, wallet.CreatedAt);
            return CreatedAtAction(nameof(GetWallets), new { id = wallet.Id }, response);
        }

        /// <summary>
        /// PUT /api/wallets/{id}/budget
        /// Cập nhật tổng ngân sách chi tiêu trong tháng (Monthly Budget)
        /// </summary>
        [HttpPut("{id:guid}/budget")]
        public async Task<ActionResult<WalletResponse>> UpdateBudget(Guid id, [FromBody] UpdateBudgetRequest request)
        {
            var wallet = await _context.Wallets.FindAsync(id);
            if (wallet == null)
                return NotFound(new { message = $"Không tìm thấy ví với ID: {id}." });

            wallet.MonthlyBudget = Math.Max(0, request.MonthlyBudget);
            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);

            var currentBalance = await _context.Transactions
                .Where(t => t.WalletId == id)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var monthlySpent = Math.Abs(await _context.Transactions
                .Where(t => t.WalletId == id && t.Amount < 0 && t.TransactionDate >= startOfMonth && t.TransactionDate < endOfMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m);

            return Ok(new WalletResponse(
                wallet.Id,
                wallet.Name,
                currentBalance,
                wallet.MonthlyBudget,
                monthlySpent,
                wallet.CreatedAt
            ));
        }

        /// <summary>
        /// DELETE /api/wallets/{id}
        /// Xóa/Hủy ví tài chính cùng toàn bộ giao dịch liên quan
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteWallet(Guid id)
        {
            var wallet = await _context.Wallets.Include(w => w.Transactions).FirstOrDefaultAsync(w => w.Id == id);
            if (wallet == null)
                return NotFound(new { message = $"Không tìm thấy ví với ID: {id}." });

            _context.Wallets.Remove(wallet);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// GET /api/wallets/{id}/transactions
        /// Lấy danh sách lịch sử giao dịch của ví (mới nhất lên đầu)
        /// </summary>
        [HttpGet("{id:guid}/transactions")]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetTransactions(Guid id)
        {
            var walletExists = await _context.Wallets.AnyAsync(w => w.Id == id);
            if (!walletExists)
                return NotFound(new { message = $"Không tìm thấy ví với ID: {id}." });

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.WalletId == id)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Select(t => new TransactionResponse(
                    t.Id,
                    t.WalletId,
                    t.Amount,
                    t.Category ?? "Khác",
                    t.Note,
                    t.TransactionDate,
                    0m, // Not needed for history listing
                    t.CreatedAt
                ))
                .ToListAsync();

            return Ok(transactions);
        }

        /// <summary>
        /// POST /api/wallets/{id}/transactions
        /// Ghi nhận giao dịch (chi tiêu hoặc nạp tiền) với Số tiền, Danh mục, Ghi chú, Ngày giao dịch
        /// </summary>
        [HttpPost("{id:guid}/transactions")]
        public async Task<ActionResult<TransactionResponse>> CreateTransaction(
            Guid id, 
            [FromBody] CreateTransactionRequest request)
        {
            if (request.Amount == 0)
                return BadRequest(new { message = "Số tiền giao dịch phải khác 0." });

            var wallet = await _context.Wallets.FindAsync(id);
            if (wallet == null)
                return NotFound(new { message = $"Không tìm thấy ví với ID: {id}." });

            var transactionDate = request.TransactionDate ?? DateTime.UtcNow;

            var transaction = new Transaction
            {
                WalletId = id,
                Amount = request.Amount,
                Category = string.IsNullOrWhiteSpace(request.Category) ? "Khác" : request.Category.Trim(),
                Note = request.Note?.Trim(),
                TransactionDate = transactionDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Tính số dư mới sau khi thêm giao dịch
            var newBalance = await _context.Transactions
                .Where(t => t.WalletId == id)
                .SumAsync(t => t.Amount);

            return Ok(new TransactionResponse(
                transaction.Id,
                transaction.WalletId,
                transaction.Amount,
                transaction.Category,
                transaction.Note,
                transaction.TransactionDate,
                newBalance,
                transaction.CreatedAt
            ));
        }
    }
}
