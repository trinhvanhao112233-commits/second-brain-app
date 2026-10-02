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

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
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

                    if (response.IsSuccessStatusCode)
                    {
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
                            var cleanJson = text.Trim();
                            if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                            if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                            if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);

                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var parsedOutput = JsonSerializer.Deserialize<AiActionOutput>(cleanJson.Trim(), options);
                            if (parsedOutput != null)
                            {
                                return parsedOutput;
                            }
                        }
                    }
                    else
                    {
                        var errorDetails = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"[Gemini API Error] {response.StatusCode}: {errorDetails}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[GeminiAiParserService] Ngoại lệ khi gọi Gemini API. Đang dùng fallback NLP.");
                }
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
  ""amount"": number hoặc null (Lưu ý số tiền luôn là số dương > 0),
  ""category"": string hoặc null (Với chi tiêu: 'Ăn uống', 'Đi lại', 'Mua sắm', 'Hóa đơn', 'Giải trí', 'Y tế', 'Khác'. Với sự kiện: 'Học tập', 'Họp', 'Work', 'Personal', 'Health', 'Important'),
  ""note"": string hoặc null,
  ""walletName"": string hoặc null,
  ""title"": string hoặc null (Tiêu đề ngắn gọn, xúc tích của sự kiện hoặc công việc, ví dụ: 'Học môn thiết kế và phát triển', 'Họp team', bỏ qua các từ lệnh như 'hãy điền cho tôi', 'nhắc tôi'),
  ""description"": string hoặc null,
  ""startTime"": string ISO 8601 (yyyy-MM-ddTHH:mm:ss) hoặc null (ví dụ bắt đầu lúc 7h sáng mai thì là yyyy-MM-ddT07:00:00),
  ""endTime"": string ISO 8601 (yyyy-MM-ddTHH:mm:ss) hoặc null (ví dụ kết thúc lúc 9h sáng thì là yyyy-MM-ddT09:00:00. ĐẶC BIỆT CHÚ Ý: nếu người dùng nói rõ giờ kết thúc thì phải lấy chính xác giờ kết thúc đó),
  ""priority"": ""High"" | ""Medium"" | ""Low"",
  ""replyMessage"": string (Câu phản hồi ngắn gọn, thân thiện, xác nhận cho người dùng bằng tiếng Việt)
}}

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
            if (lower.Contains("hẹn") || lower.Contains("họp") || lower.Contains("lịch") || lower.Contains("đi khám") || 
                lower.Contains("học") || lower.Contains("bắt đầu") || lower.Contains("kết thúc") || lower.Contains("tiết"))
            {
                var now = DateTime.Now;
                var eventDate = now;
                if (lower.Contains("sáng mai") || lower.Contains("ngày mai") || lower.Contains("chiều mai") || lower.Contains("tối mai"))
                {
                    eventDate = now.AddDays(1);
                }
                else if (lower.Contains("hôm nay") || lower.Contains("tối nay") || lower.Contains("chiều nay"))
                {
                    eventDate = now;
                }
                else if (lower.Contains("mai"))
                {
                    eventDate = now.AddDays(1);
                }
                else if (lower.Contains("kia"))
                {
                    eventDate = now.AddDays(2);
                }

                // Trích xuất giờ bắt đầu
                int startHour = 8;
                int startMinute = 0;
                var startMatch = Regex.Match(lower, @"(bắt đầu\s*(từ)?|từ)\s*(\d{1,2})\s*(h|giờ|g)(\s*(\d{1,2})\s*(p|phút)?)?");
                if (startMatch.Success && int.TryParse(startMatch.Groups[3].Value, out var sH))
                {
                    startHour = sH;
                    if (!string.IsNullOrWhiteSpace(startMatch.Groups[6].Value) && int.TryParse(startMatch.Groups[6].Value, out var sM))
                    {
                        startMinute = sM;
                    }
                }
                else
                {
                    var anyHourMatch = Regex.Match(lower, @"(\d{1,2})\s*(h|giờ|g)");
                    if (anyHourMatch.Success && int.TryParse(anyHourMatch.Groups[1].Value, out var firstH))
                    {
                        startHour = firstH;
                    }
                }

                if ((lower.Contains("chiều") || lower.Contains("tối")) && startHour < 12)
                {
                    startHour += 12;
                }

                // Trích xuất giờ kết thúc
                int endHour = startHour + 1;
                int endMinute = startMinute;
                var endMatch = Regex.Match(lower, @"(kết thúc\s*(lúc|vào)?|đến|tới)\s*(\d{1,2})\s*(h|giờ|g)(\s*(\d{1,2})\s*(p|phút)?)?");
                if (endMatch.Success && int.TryParse(endMatch.Groups[3].Value, out var eH))
                {
                    endHour = eH;
                    if ((lower.Contains("chiều") || lower.Contains("tối")) && endHour < 12)
                    {
                        endHour += 12;
                    }
                    if (!string.IsNullOrWhiteSpace(endMatch.Groups[6].Value) && int.TryParse(endMatch.Groups[6].Value, out var eM))
                    {
                        endMinute = eM;
                    }
                }

                var start = new DateTime(eventDate.Year, eventDate.Month, eventDate.Day, startHour, startMinute, 0);
                var end = new DateTime(eventDate.Year, eventDate.Month, eventDate.Day, endHour, endMinute, 0);
                if (end <= start)
                {
                    end = start.AddHours(1);
                }

                // Làm sạch tiêu đề sự kiện
                var cleanTitle = Regex.Replace(raw, @"(?i)(hãy\s+điền\s+cho\s+tôi|nhắc\s+tôi|thêm\s+lịch|đặt\s+lịch)\s*", "").Trim();

                var category = "Personal";
                if (lower.Contains("học") || lower.Contains("thiết kế") || lower.Contains("môn")) category = "Học tập";
                else if (lower.Contains("họp") || lower.Contains("công việc") || lower.Contains("dự án")) category = "Work";

                return new AiActionOutput
                {
                    ActionType = "add_event",
                    Title = cleanTitle,
                    StartTime = start.ToString("yyyy-MM-ddTHH:mm:ss"),
                    EndTime = end.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Category = category,
                    ReplyMessage = $"Đã lên lịch '{cleanTitle}' từ {start:HH:mm} đến {end:HH:mm} ngày {start:dd/MM/yyyy}!"
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
