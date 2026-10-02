using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonalFinance.API.DTOs;

namespace PersonalFinance.API.Services
{
    public interface IAiParserService
    {
        Task<AiActionOutput> ParseUserMessageAsync(string message, string userContextInfo);
    }

    public class GeminiAiParserService : IAiParserService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiAiParserService> _logger;

        public GeminiAiParserService(IConfiguration config, HttpClient httpClient, ILogger<GeminiAiParserService> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AiActionOutput> ParseUserMessageAsync(string message, string userContextInfo)
        {
            var apiKey = _config["Gemini:ApiKey"];
            var model = _config["Gemini:Model"] ?? "gemini-1.5-flash";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("[GeminiAiParserService] Không tìm thấy Gemini:ApiKey trong appsettings.json. Tự động dùng bộ phân tích NLP nội bộ.");
                return FallbackRuleBasedParser(message);
            }

            try
            {
                var prompt = BuildPrompt(message, userContextInfo);
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        responseMimeType = "application/json"
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[Gemini API Error] {response.StatusCode}: {errorDetails}");
                    return FallbackRuleBasedParser(message);
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                var text = root
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Clean json if wrapped in ```json
                    var cleanJson = text.Trim();
                    if (cleanJson.StartsWith("```json"))
                    {
                        cleanJson = cleanJson.Substring(7);
                    }
                    if (cleanJson.StartsWith("```"))
                    {
                        cleanJson = cleanJson.Substring(3);
                    }
                    if (cleanJson.EndsWith("```"))
                    {
                        cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                    }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var parsedOutput = JsonSerializer.Deserialize<AiActionOutput>(cleanJson.Trim(), options);
                    if (parsedOutput != null)
                    {
                        return parsedOutput;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GeminiAiParserService] Ngoại lệ khi gọi Gemini API. Đang dùng fallback NLP.");
            }

            return FallbackRuleBasedParser(message);
        }

        private string BuildPrompt(string userMessage, string userContextInfo)
        {
            var now = DateTime.Now;
            var currentTimeString = now.ToString("yyyy-MM-dd HH:mm:ss (dddd)");

            return $@"
Bạn là trợ lý AI thông minh tích hợp trong hệ thống quản lý cá nhân 'Second Brain' của người dùng.
Thời gian hiện tại: {currentTimeString}.
Thông tin ngữ cảnh người dùng:
{userContextInfo}

Nhiệm vụ của bạn:
Phân tích yêu cầu bằng ngôn ngữ tự nhiên tiếng Việt của người dùng và chuyển thành một JSON duy nhất phù hợp với schema sau:
{{
  ""actionType"": ""add_expense"" | ""add_income"" | ""add_event"" | ""add_task"" | ""chat"",
  ""amount"": number hoặc null (ví dụ 70000 nếu nói 70k, 150000 nếu nói 150k, 1.5 triệu là 1500000. Lưu ý số tiền luôn là số dương > 0),
  ""category"": string hoặc null (Với chi tiêu: 'Ăn uống', 'Đi lại', 'Mua sắm', 'Hóa đơn', 'Giải trí', 'Y tế', hoặc 'Khác'. Với sự kiện: 'Personal', 'Work', 'Health', 'Important'),
  ""note"": string hoặc null (Mô tả món tiền vừa chi/thu, ví dụ: 'Ăn sáng phở bò', 'Đi taxi Grab'),
  ""walletName"": string hoặc null (Nếu người dùng nói rõ ví như 'ví chính', 'ví tiết kiệm' hoặc để null nếu dùng ví mặc định),
  ""title"": string hoặc null (Tiêu đề sự kiện hoặc công việc, ví dụ: 'Họp team dự án', 'Đi khám nha khoa'),
  ""description"": string hoặc null,
  ""startTime"": string ISO 8601 (yyyy-MM-ddTHH:mm:ss) hoặc null (Dành cho sự kiện lịch trình, tính toán từ ngữ cảnh như '9h sáng mai', 'chiều nay 15h'),
  ""endTime"": string ISO 8601 (yyyy-MM-ddTHH:mm:ss) hoặc null (mặc định cộng thêm 1 giờ sau startTime nếu không nói rõ),
  ""priority"": ""High"" | ""Medium"" | ""Low"" (cho công việc task),
  ""replyMessage"": string (Câu phản hồi ngắn gọn, thân thiện, xác nhận hành động cho người dùng bằng tiếng Việt)
}}

Quy tắc phân loại:
1. 'Đã ăn sáng hết 70k', 'mua cafe 35k', 'đổ xăng 50k', 'chi 120k tiền đi chợ' -> actionType = 'add_expense'.
2. 'Vừa nhận lương 15 triệu', 'được thưởng 500k', 'nạp vào ví 200k' -> actionType = 'add_income'.
3. 'Chiều mai 14h có hẹn gặp khách hàng ở The Coffee House', 'ngày mai 9h họp team' -> actionType = 'add_event'. Tính toán đúng startTime và endTime.
4. 'Nhớ mua tài liệu', 'cần nộp báo cáo trước thứ 6' -> actionType = 'add_task'.
5. Các câu chào hỏi hoặc hỏi đáp thông thường -> actionType = 'chat'.

Chỉ trả về JSON thuần, không kèm markdown hoặc giải thích bên ngoài.

Tin nhắn của người dùng:
""{userMessage}""
";
        }

        /// <summary>
        /// Bộ phân tích cục bộ Regex NLP khi chưa có ApiKey hoặc khi mạng lỗi
        /// </summary>
        public AiActionOutput FallbackRuleBasedParser(string message)
        {
            var raw = message.Trim();
            var lower = raw.ToLower();

            // 1. Kiểm tra Chi tiêu (Expense)
            // Ví dụ: "Đã ăn sáng hết 70k", "chi 50k mua cafe", "ăn trưa 40.000đ"
            var isExpense = lower.Contains("ăn") || lower.Contains("uống") || lower.Contains("mua") ||
                            lower.Contains("hết") || lower.Contains("chi") || lower.Contains("tiêu") ||
                            lower.Contains("trả tiền") || lower.Contains("đổ xăng");

            var isIncome = lower.Contains("nhận") || lower.Contains("lương") || lower.Contains("thưởng") ||
                           lower.Contains("nạp") || lower.Contains("thu nhập");

            var amount = ExtractAmount(lower);

            if (isExpense && amount > 0)
            {
                var category = DetectCategory(lower);
                return new AiActionOutput
                {
                    ActionType = "add_expense",
                    Amount = amount,
                    Category = category,
                    Note = CleanNote(raw),
                    ReplyMessage = $"Đã ghi nhận khoản chi tiêu {amount:N0}đ ({category}) vào ví cho bạn!"
                };
            }

            if (isIncome && amount > 0)
            {
                return new AiActionOutput
                {
                    ActionType = "add_income",
                    Amount = amount,
                    Category = "Nạp tiền",
                    Note = CleanNote(raw),
                    ReplyMessage = $"Đã cộng thêm {amount:N0}đ vào ví thành công!"
                };
            }

            // 2. Kiểm tra Sự kiện lịch biểu
            if (lower.Contains("hẹn") || lower.Contains("họp") || lower.Contains("lịch") || lower.Contains("đi khám") || lower.Contains("bay"))
            {
                var now = DateTime.Now;
                var eventDate = now;
                if (lower.Contains("mai")) eventDate = now.AddDays(1);
                else if (lower.Contains("kia")) eventDate = now.AddDays(2);

                var hour = 9;
                var hourMatch = Regex.Match(lower, @"(\d{1,2})\s*(h|giờ)");
                if (hourMatch.Success && int.TryParse(hourMatch.Groups[1].Value, out var parsedHour))
                {
                    if (lower.Contains("chiều") && parsedHour < 12) parsedHour += 12;
                    if (lower.Contains("tối") && parsedHour < 12) parsedHour += 12;
                    hour = parsedHour;
                }

                var start = new DateTime(eventDate.Year, eventDate.Month, eventDate.Day, hour, 0, 0);
                var end = start.AddHours(1);

                return new AiActionOutput
                {
                    ActionType = "add_event",
                    Title = raw,
                    StartTime = start.ToString("yyyy-MM-ddTHH:mm:ss"),
                    EndTime = end.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Category = lower.Contains("họp") || lower.Contains("công việc") ? "Work" : "Personal",
                    ReplyMessage = $"Đã tạo lịch hẹn: '{raw}' vào lúc {start:HH:mm dd/MM/yyyy}!"
                };
            }

            // 3. Kiểm tra Công việc (Task)
            if (lower.StartsWith("cần ") || lower.StartsWith("nhớ ") || lower.Contains("phải làm") || lower.Contains("nhiệm vụ"))
            {
                return new AiActionOutput
                {
                    ActionType = "add_task",
                    Title = raw.Replace("cần ", "").Replace("nhớ ", "").Trim(),
                    Priority = lower.Contains("gấp") || lower.Contains("quan trọng") ? "High" : "Medium",
                    ReplyMessage = $"Đã thêm việc cần làm: '{raw}' vào danh sách công việc!"
                };
            }

            // 4. Mặc định là trò chuyện / trợ giúp
            return new AiActionOutput
            {
                ActionType = "chat",
                ReplyMessage = "Chào bạn! Tôi là trợ lý AI Second Brain. Bạn có thể nói những câu như: 'Đã ăn sáng hết 70k', 'Vừa nạp ví 200k', hoặc '9h sáng mai họp team' để tôi tự động cập nhật hệ thống nhé!"
            };
        }

        private decimal ExtractAmount(string text)
        {
            // Tìm các mẫu: 70k, 70 k, 70.000, 70000, 1.5tr, 1.5 triệu
            var trMatch = Regex.Match(text, @"([\d\.,]+)\s*(tr|triệu|trieu)");
            if (trMatch.Success)
            {
                var numStr = trMatch.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var trVal))
                {
                    return trVal * 1_000_000m;
                }
            }

            var kMatch = Regex.Match(text, @"([\d\.,]+)\s*k\b");
            if (kMatch.Success)
            {
                var numStr = kMatch.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var kVal))
                {
                    return kVal * 1_000m;
                }
            }

            var fullMatch = Regex.Match(text, @"(\d{1,3}([\.,]\d{3})+|\d{4,})");
            if (fullMatch.Success)
            {
                var cleaned = fullMatch.Groups[1].Value.Replace(".", "").Replace(",", "");
                if (decimal.TryParse(cleaned, out var fullVal))
                {
                    return fullVal;
                }
            }

            return 0m;
        }

        private string DetectCategory(string text)
        {
            if (text.Contains("ăn") || text.Contains("uống") || text.Contains("cơm") || text.Contains("phở") ||
                text.Contains("bún") || text.Contains("cafe") || text.Contains("cà phê") || text.Contains("bánh"))
                return "Ăn uống";

            if (text.Contains("xăng") || text.Contains("grab") || text.Contains("xe") || text.Contains("taxi") || text.Contains("vé tàu"))
                return "Đi lại";

            if (text.Contains("mua") || text.Contains("áo") || text.Contains("quần") || text.Contains("shopee") || text.Contains("lazada"))
                return "Mua sắm";

            if (text.Contains("điện") || text.Contains("nước") || text.Contains("internet") || text.Contains("hóa đơn") || text.Contains("tiền nhà"))
                return "Hóa đơn";

            if (text.Contains("phim") || text.Contains("game") || text.Contains("du lịch") || text.Contains("hát"))
                return "Giải trí";

            if (text.Contains("thuốc") || text.Contains("khám") || text.Contains("bệnh"))
                return "Y tế";

            return "Ăn uống";
        }

        private string CleanNote(string text)
        {
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}
