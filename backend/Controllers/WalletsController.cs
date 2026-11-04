using PersonalFinance.API.Security;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WalletsController : OwnedController
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
        public async Task<ActionResult<IEnumerable<WalletResponse>>> GetWallets()
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
                .OrderByDescending(w => w.IsMaster)
                .ThenByDescending(w => w.CreatedAt)
                .Select(w => new WalletResponse(
                    w.Id,
                    w.Name,
                    w.Transactions.Sum(t => (decimal?)t.Amount) ?? 0m,
                    w.MonthlyBudget,
                    // Chi tiêu trong tháng là các giao dịch Amount < 0
                    Math.Abs(w.Transactions
                        .Where(t => !t.Category.StartsWith("Chuyển tiền") && t.Category != "Phân bổ ví con" && t.Amount < 0 && t.TransactionDate >= startOfMonth && t.TransactionDate < endOfMonth)
                        .Sum(t => (decimal?)t.Amount) ?? 0m),
                    w.IsMaster,
                    w.CreatedAt
                ))
                .ToListAsync();

            return Ok(wallets);
        }

        /// <summary>
        /// POST /api/wallets
        /// Nhận JSON { "name": "Tên ví", "monthlyBudget": 5000000, "isMaster": true/false } để tạo ví mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<WalletResponse>> CreateWallet(
            [FromBody] CreateWalletRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Nếu đặt làm ví tổng, hủy ví tổng cũ của user này
            if (request.IsMaster)
            {
                var existingMasters = await _context.Wallets
                    .Where(w => w.UserId == userId && w.IsMaster)
                    .ToListAsync();
                foreach (var em in existingMasters)
                {
                    em.IsMaster = false;
                }
            }

            if (request.Id.HasValue && await _context.Wallets.AnyAsync(w => w.Id == request.Id.Value)) {
                var saved = await _context.Wallets.FirstAsync(w => w.Id == request.Id.Value);
                return Ok(new WalletResponse(saved.Id, saved.Name, await _context.Transactions.Where(t => t.WalletId == saved.Id).SumAsync(t => (decimal?)t.Amount) ?? 0, saved.MonthlyBudget, 0, saved.IsMaster, saved.CreatedAt));
            }
            var wallet = new Wallet
            {
                Id = request.Id ?? Guid.NewGuid(),
                UserId = userId,
                Name = request.Name.Trim(),
                MonthlyBudget = request.MonthlyBudget >= 0 ? request.MonthlyBudget : 0m,
                IsMaster = request.IsMaster,
                CreatedAt = DateTime.UtcNow
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            var response = new WalletResponse(wallet.Id, wallet.Name, 0m, wallet.MonthlyBudget, 0m, wallet.IsMaster, wallet.CreatedAt);
            return CreatedAtAction(nameof(GetWallets), new { id = wallet.Id }, response);
        }

        /// <summary>
        /// PUT /api/wallets/{id}/master
        /// Đặt hoặc hủy quyền Ví Tổng cho một ví
        /// </summary>
        [HttpPut("{id:guid}/master")]
        public async Task<ActionResult<WalletResponse>> SetMasterWallet(
            Guid id,
            [FromBody] SetMasterWalletRequest request)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (wallet == null)
                return NotFound(new { message = $"Không tìm thấy ví với ID: {id}." });

            if (request.IsMaster)
            {
                // Hủy master của các ví khác của cùng user
                var otherMasters = await _context.Wallets
                    .Where(w => w.UserId == userId && w.Id != id && w.IsMaster)
                    .ToListAsync();
                foreach (var om in otherMasters)
                {
                    om.IsMaster = false;
                }
                wallet.IsMaster = true;
            }
            else
            {
                wallet.IsMaster = false;
            }

            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);

            var currentBalance = await _context.Transactions
                .Where(t => t.WalletId == id)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var monthlySpent = Math.Abs(await _context.Transactions
                .Where(t => t.WalletId == id && !t.Category.StartsWith("Chuyển tiền") && t.Category != "Phân bổ ví con" && t.Amount < 0 && t.TransactionDate >= startOfMonth && t.TransactionDate < endOfMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m);

            return Ok(new WalletResponse(
                wallet.Id,
                wallet.Name,
                currentBalance,
                wallet.MonthlyBudget,
                monthlySpent,
                wallet.IsMaster,
                wallet.CreatedAt
            ));
        }

        /// <summary>
        /// PUT /api/wallets/{id}/budget
        /// Cập nhật tổng ngân sách chi tiêu trong tháng (Monthly Budget)
        /// </summary>
        [HttpPut("{id:guid}/budget")]
        public async Task<ActionResult<WalletResponse>> UpdateBudget(Guid id, [FromBody] UpdateBudgetRequest request)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
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
                .Where(t => t.WalletId == id && !t.Category.StartsWith("Chuyển tiền") && t.Category != "Phân bổ ví con" && t.Amount < 0 && t.TransactionDate >= startOfMonth && t.TransactionDate < endOfMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m);

            return Ok(new WalletResponse(
                wallet.Id,
                wallet.Name,
                currentBalance,
                wallet.MonthlyBudget,
                monthlySpent,
                wallet.IsMaster,
                wallet.CreatedAt
            ));
        }

        /// <summary>
        /// POST /api/wallets/transfer
        /// Chuyển tiền từ Ví này sang Ví khác (ví dụ: Phân bổ từ Ví Tổng sang Ví Con)
        /// </summary>
        [HttpPost("transfer")]
        public async Task<IActionResult> TransferFunds(
            [FromBody] TransferWalletRequest request)
        {
            if (request.SourceWalletId == request.TargetWalletId)
                return BadRequest(new { message = "Ví nguồn và ví đích không được trùng nhau." });

            if (request.Amount <= 0)
                return BadRequest(new { message = "Số tiền chuyển phải lớn hơn 0." });

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () => {
            await using var databaseTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var transferId = request.Id ?? Guid.NewGuid();
            if (await _context.Transactions.AnyAsync(x => x.TransferId == transferId)) return Ok(new { message = "Giao dịch đã được ghi nhận." });
            var sourceWallet = await _context.Wallets.FirstOrDefaultAsync(x => x.Id == request.SourceWalletId && x.UserId == userId);
            var targetWallet = await _context.Wallets.FirstOrDefaultAsync(x => x.Id == request.TargetWalletId && x.UserId == userId);

            if (sourceWallet == null || targetWallet == null)
                return NotFound(new { message = "Không tìm thấy ví nguồn hoặc ví đích." });

            // Kiểm tra số dư ví nguồn
            var sourceBalance = await _context.Transactions
                .Where(t => t.WalletId == request.SourceWalletId)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            if (sourceBalance < request.Amount)
            {
                return BadRequest(new { 
                    message = $"Số dư ví '{sourceWallet.Name}' không đủ ({sourceBalance:N0} VNĐ < {request.Amount:N0} VNĐ)." 
                });
            }

            var txDate = request.TransactionDate ?? DateTime.UtcNow;
            var note = string.IsNullOrWhiteSpace(request.Note) ? "Chuyển tiền giữa các ví" : request.Note.Trim();

            // Trừ tiền ví nguồn
            var outTx = new Transaction
            {
                TransferId = transferId,
                WalletId = request.SourceWalletId,
                Amount = -Math.Abs(request.Amount),
                Category = "Chuyển tiền đi",
                Note = $"Chuyển sang [{targetWallet.Name}]: {note}",
                TransactionDate = txDate,
                CreatedAt = DateTime.UtcNow
            };

            // Cộng tiền vào ví đích
            var inTx = new Transaction
            {
                TransferId = transferId,
                WalletId = request.TargetWalletId,
                Amount = Math.Abs(request.Amount),
                Category = "Nạp tiền từ ví khác",
                Note = $"Nhận từ [{sourceWallet.Name}]: {note}",
                TransactionDate = txDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.AddRange(outTx, inTx);
            await _context.SaveChangesAsync();
            await databaseTransaction.CommitAsync();

            return Ok(new { message = "Chuyển tiền thành công!", sourceWalletId = sourceWallet.Id, targetWalletId = targetWallet.Id, amount = request.Amount });
            });
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

            if (wallet.Transactions.Any(x => x.TransferId != null || x.DebtRecordId != null)) return BadRequest(new { message = "Hãy xóa giao dịch chuyển ví hoặc trả nợ liên quan trước khi xóa ví này." });
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
        /// Ghi nhận giao dịch (chi tiêu hoặc nạp tiền) với Số tiền, Danh mục, Ghi chú, Ngày giao dịch.
        /// Hỗ trợ DeductFromMaster: Khi nạp vào ví con, tự động trích tiền từ Ví Tổng nếu chọn.
        /// </summary>
        [HttpPost("{id:guid}/transactions")]
        public async Task<ActionResult<TransactionResponse>> CreateTransaction(
            Guid id,
            [FromBody] CreateTransactionRequest request)
        {
            if (request.Amount == 0)
                return BadRequest(new { message = "Số tiền giao dịch phải khác 0." });

            var wallet = await _context.Wallets.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (wallet == null)
                return NotFound(new { message = $"Không tìm thấy ví với ID: {id}." });

            if (request.Id.HasValue) {
                var saved = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == request.Id.Value && t.WalletId == id);
                if (saved != null) return Ok(new TransactionResponse(saved.Id, id, saved.Amount, saved.Category, saved.Note, saved.TransactionDate, 0, saved.CreatedAt));
            }
            var transactionDate = request.TransactionDate ?? DateTime.UtcNow;

            // Nếu nạp tiền (Amount > 0) vào ví con và có yêu cầu DeductFromMaster
            if (request.Amount > 0 && request.DeductFromMaster && !wallet.IsMaster)
            {
                var masterWallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.IsMaster);

                if (masterWallet == null)
                {
                    return BadRequest(new { message = "Chưa thiết lập Ví Tổng để trích tiền!" });
                }

                var masterBalance = await _context.Transactions
                    .Where(t => t.WalletId == masterWallet.Id)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m;

                if (masterBalance < request.Amount)
                {
                    return BadRequest(new { 
                        message = $"Số dư Ví Tổng '{masterWallet.Name}' không đủ để trích ({masterBalance:N0} VNĐ < {request.Amount:N0} VNĐ)." 
                    });
                }

                // Ghi nhận trừ tiền ở Ví Tổng
                var masterOutTx = new Transaction
                {
                    WalletId = masterWallet.Id,
                    Amount = -Math.Abs(request.Amount),
                    Category = "Phân bổ ví con",
                    Note = $"Trích nạp cho [{wallet.Name}]: {(string.IsNullOrWhiteSpace(request.Note) ? "Nạp tiền" : request.Note)}",
                    TransactionDate = transactionDate,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Transactions.Add(masterOutTx);
            }

            var transaction = new Transaction
            {
                Id = request.Id ?? Guid.NewGuid(),
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

        /// <summary>
        /// DELETE /api/wallets/transactions/{txId}
        /// Xóa một giao dịch cụ thể
        /// </summary>
        [HttpDelete("transactions/{txId:guid}")]
        public async Task<IActionResult> DeleteTransaction(Guid txId)
        {
            var tx = await _context.Transactions.FirstOrDefaultAsync(x => x.Id == txId && x.Wallet!.UserId == userId);
            if (tx == null)
                return NotFound(new { message = "Không tìm thấy giao dịch." });

            if (tx.DebtRecordId.HasValue) {
                var debt = await _context.DebtRecords.FirstOrDefaultAsync(x => x.Id == tx.DebtRecordId);
                if (debt != null) { debt.PaidAmount = Math.Max(0, debt.PaidAmount - Math.Abs(tx.Amount)); debt.IsCompleted = debt.PaidAmount >= debt.TotalAmount; }
            }
            if (tx.TransferId.HasValue) {
                var linked = await _context.Transactions.Where(x => x.TransferId == tx.TransferId).ToListAsync();
                _context.Transactions.RemoveRange(linked);
            } else _context.Transactions.Remove(tx);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// DELETE /api/wallets/reset-all
        /// Xóa sạch dữ liệu tài chính (Toàn bộ giao dịch và các ví tiền)
        /// </summary>
        [HttpDelete("reset-all")]
        public async Task<IActionResult> ResetAllFinancialData()
        {
            try
            {
                if (userId.HasValue)
                {
                    var userWalletIds = await _context.Wallets
                        .Where(w => w.UserId == userId.Value)
                        .Select(w => w.Id)
                        .ToListAsync();

                    var userTransactions = await _context.Transactions
                        .Where(t => userWalletIds.Contains(t.WalletId))
                        .ToListAsync();

                    _context.Transactions.RemoveRange(userTransactions);

                    var userWallets = await _context.Wallets
                        .Where(w => w.UserId == userId.Value)
                        .ToListAsync();

                    _context.Wallets.RemoveRange(userWallets);
                }
                else { return Unauthorized(); }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã xóa toàn bộ dữ liệu ví và giao dịch thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi xóa dữ liệu: {ex.Message}" });
            }
        }
    }
}
