using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;
using PersonalFinance.API.DTOs;
using PersonalFinance.API.Models;
using PersonalFinance.API.Services;

namespace PersonalFinance.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiAssistantController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAiParserService _aiParserService;

        public AiAssistantController(AppDbContext context, IAiParserService aiParserService)
        {
            _context = context;
            _aiParserService = aiParserService;
        }

        [HttpPost("chat")]
        public async Task<ActionResult<AiProcessResponse>> ProcessMessage(
            [FromHeader(Name = "X-User-Id")] Guid? userId,
            [FromBody] AiProcessRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });
            }

            // 1. Thu thập ngữ cảnh hiện tại của người dùng (Ví tiền, Danh mục, Lịch biểu hôm nay)
            var walletsQuery = _context.Wallets.AsNoTracking().AsQueryable();
            if (userId.HasValue)
            {
                walletsQuery = walletsQuery.Where(w => w.UserId == userId.Value);
            }
            var userWallets = await walletsQuery.ToListAsync();

            var walletContext = string.Join(", ", userWallets.Select(w => $"'{w.Name}' (ID: {w.Id})"));
            var contextInfo = $"Người dùng có các ví sau: [{walletContext}].";

            // 2. Gọi AI Parser (Gemini hoặc Fallback NLP)
            var actionOutput = await _aiParserService.ParseUserMessageAsync(request.Message, contextInfo);

            // 3. Thực thi hành động tương ứng vào Database
            switch (actionOutput.ActionType)
            {
                case "add_expense":
                case "add_income":
                {
                    if (!actionOutput.Amount.HasValue || actionOutput.Amount.Value <= 0)
                    {
                        return Ok(new AiProcessResponse(
                            Success: false,
                            Reply: "Tôi hiểu bạn muốn ghi nhận giao dịch, nhưng chưa nhận diện được số tiền cụ thể (ví dụ: 70k, 50000đ). Bạn vui lòng nói rõ số tiền nhé!",
                            ActionExecuted: "none",
                            Data: null
                        ));
                    }

                    // Tìm ví phù hợp
                    Wallet? targetWallet = null;
                    if (!string.IsNullOrWhiteSpace(actionOutput.WalletName))
                    {
                        targetWallet = userWallets.FirstOrDefault(w => 
                            w.Name.ToLower().Contains(actionOutput.WalletName.ToLower()));
                    }

                    if (targetWallet == null && request.DefaultWalletId.HasValue)
                    {
                        targetWallet = userWallets.FirstOrDefault(w => w.Id == request.DefaultWalletId.Value);
                    }

                    if (targetWallet == null)
                    {
                        targetWallet = userWallets.FirstOrDefault();
                    }

                    // Nếu chưa có ví nào thì tự động tạo 1 ví mặc định
                    if (targetWallet == null)
                    {
                        targetWallet = new Wallet
                        {
                            UserId = userId,
                            Name = "Ví Chính",
                            MonthlyBudget = 5000000m,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Wallets.Add(targetWallet);
                        await _context.SaveChangesAsync();
                    }

                    var isExpense = actionOutput.ActionType == "add_expense";
                    var finalAmount = isExpense ? -Math.Abs(actionOutput.Amount.Value) : Math.Abs(actionOutput.Amount.Value);

                    var transaction = new Transaction
                    {
                        WalletId = targetWallet.Id,
                        Amount = finalAmount,
                        Category = string.IsNullOrWhiteSpace(actionOutput.Category) 
                            ? (isExpense ? "Ăn uống" : "Nạp tiền") 
                            : actionOutput.Category,
                        Note = string.IsNullOrWhiteSpace(actionOutput.Note) ? request.Message : actionOutput.Note,
                        TransactionDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Transactions.Add(transaction);
                    await _context.SaveChangesAsync();

                    // Tính lại số dư mới của ví
                    var newBalance = await _context.Transactions
                        .Where(t => t.WalletId == targetWallet.Id)
                        .SumAsync(t => t.Amount);

                    var reply = string.IsNullOrWhiteSpace(actionOutput.ReplyMessage)
                        ? (isExpense 
                            ? $"Đã trừ {Math.Abs(finalAmount):N0}đ ({transaction.Category}) từ '{targetWallet.Name}'. Số dư hiện tại: {newBalance:N0}đ."
                            : $"Đã cộng {Math.Abs(finalAmount):N0}đ vào '{targetWallet.Name}'. Số dư hiện tại: {newBalance:N0}đ.")
                        : actionOutput.ReplyMessage;

                    return Ok(new AiProcessResponse(
                        Success: true,
                        Reply: reply,
                        ActionExecuted: "transaction",
                        Data: new
                        {
                            transactionId = transaction.Id,
                            walletId = targetWallet.Id,
                            walletName = targetWallet.Name,
                            amount = finalAmount,
                            category = transaction.Category,
                            note = transaction.Note,
                            newBalance = newBalance
                        }
                    ));
                }

                case "add_event":
                {
                    var title = !string.IsNullOrWhiteSpace(actionOutput.Title) ? actionOutput.Title : request.Message;
                    
                    DateTime startTime = DateTime.Now.AddHours(1);
                    if (!string.IsNullOrWhiteSpace(actionOutput.StartTime) && 
                        DateTime.TryParse(actionOutput.StartTime, out var parsedStart))
                    {
                        startTime = parsedStart;
                    }

                    DateTime endTime = startTime.AddHours(1);
                    if (!string.IsNullOrWhiteSpace(actionOutput.EndTime) && 
                        DateTime.TryParse(actionOutput.EndTime, out var parsedEnd))
                    {
                        endTime = parsedEnd;
                    }

                    var category = !string.IsNullOrWhiteSpace(actionOutput.Category) ? actionOutput.Category : "Personal";
                    var color = "#10b981";
                    if (category == "Học tập") color = "#f97316";
                    else if (category == "Work" || category == "Họp") color = "#3b82f6";
                    else if (category == "Important") color = "#f43f5e";

                    var newEvent = new CalendarEvent
                    {
                        UserId = userId,
                        Title = title,
                        Description = actionOutput.Description ?? request.Message,
                        StartTime = startTime,
                        EndTime = endTime,
                        Category = category,
                        Color = color,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Events.Add(newEvent);
                    await _context.SaveChangesAsync();

                    var reply = string.IsNullOrWhiteSpace(actionOutput.ReplyMessage)
                        ? $"Đã tạo lịch hẹn '{newEvent.Title}' vào lúc {startTime.ToLocalTime():HH:mm dd/MM/yyyy}!"
                        : actionOutput.ReplyMessage;

                    return Ok(new AiProcessResponse(
                        Success: true,
                        Reply: reply,
                        ActionExecuted: "event",
                        Data: newEvent
                    ));
                }

                case "add_task":
                {
                    var taskTitle = !string.IsNullOrWhiteSpace(actionOutput.Title) ? actionOutput.Title : request.Message;
                    var task = new DailyTask
                    {
                        UserId = userId,
                        Title = taskTitle,
                        Priority = actionOutput.Priority ?? "Medium",
                        IsCompleted = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Tasks.Add(task);
                    await _context.SaveChangesAsync();

                    var reply = string.IsNullOrWhiteSpace(actionOutput.ReplyMessage)
                        ? $"Đã lưu việc cần làm '{task.Title}' vào danh sách nhiệm vụ!"
                        : actionOutput.ReplyMessage;

                    return Ok(new AiProcessResponse(
                        Success: true,
                        Reply: reply,
                        ActionExecuted: "task",
                        Data: task
                    ));
                }

                default:
                {
                    return Ok(new AiProcessResponse(
                        Success: true,
                        Reply: string.IsNullOrWhiteSpace(actionOutput.ReplyMessage)
                            ? "Tôi đã ghi nhận tin nhắn của bạn. Bạn có muốn thêm khoản chi tiêu hay xếp lịch trình nào không?"
                            : actionOutput.ReplyMessage,
                        ActionExecuted: "none",
                        Data: null
                    ));
                }
            }
        }
    }
}
