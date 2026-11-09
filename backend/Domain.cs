using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;

namespace LifeOS;

public sealed class ApiError(int status, string message) : Exception(message) { public int Status { get; } = status; }
public sealed record AppUser(Guid Id, string Username, string FullName, string PasswordHash, int PasswordVersion);
public sealed record SessionTicket(Guid Id, int Version, DateTimeOffset Expires);
public sealed record StateRow(JsonObject State, int Revision);
public sealed record BankConnection(Guid Id, Guid UserId, string AccountNumber, string KeyHash);
public sealed record BankEvent(long Id, decimal Amount, DateTime OccurredAt, string Content, string Reference, string Hash);
public sealed record BankStatus(Guid? ConnectionId, string AccountNumber, long Total, DateTime? LastReceived);
public sealed record SePayPayload(
    [property: JsonPropertyName("id"), JsonRequired] long Id,
    [property: JsonPropertyName("gateway")] string? Gateway,
    [property: JsonPropertyName("accountNumber")] string? AccountNumber,
    [property: JsonPropertyName("transferType")] string? TransferType,
    [property: JsonPropertyName("transferAmount")] decimal TransferAmount,
    [property: JsonPropertyName("transactionDate")] string? TransactionDate,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("referenceCode")] string? ReferenceCode);

public sealed class SessionTokens(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("LifeOS.Session.v1");
    public string Issue(AppUser user) => protector.Protect(JsonSerializer.Serialize(new SessionTicket(user.Id, user.PasswordVersion, DateTimeOffset.UtcNow.AddDays(7))));
    public SessionTicket Read(string token) {
        try { var ticket = JsonSerializer.Deserialize<SessionTicket>(protector.Unprotect(token)); if (ticket == null || ticket.Expires <= DateTimeOffset.UtcNow) throw new Exception(); return ticket; }
        catch { throw new ApiError(401, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."); }
    }
}

public static class BankRules
{
    public static string NewKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static bool Authenticate(string? header, string hash) {
        if (header == null || !header.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase) || hash.Length != 64) return false;
        var key = header[7..]; if (key.Length != 64 || key.Any(c => !Uri.IsHexDigit(c))) return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(key)), Encoding.ASCII.GetBytes(hash));
    }
    public static BankEvent Validate(SePayPayload p, string account) {
        if (p.Id <= 0 || p.Id > 9007199254740991L) throw new ApiError(400, "ID giao dịch không hợp lệ.");
        if (!string.Equals(p.Gateway, "VietinBank", StringComparison.OrdinalIgnoreCase) || p.AccountNumber != account) throw new ApiError(400, "Ngân hàng hoặc tài khoản không khớp.");
        if (p.TransferType is not ("in" or "out")) throw new ApiError(400, "Chiều giao dịch không hợp lệ.");
        if (p.TransferAmount <= 0 || p.TransferAmount > 9007199254740991m || decimal.Truncate(p.TransferAmount) != p.TransferAmount) throw new ApiError(400, "Số tiền VNĐ không hợp lệ.");
        if (!DateTime.TryParseExact(p.TransactionDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) throw new ApiError(400, "Ngày giao dịch không hợp lệ.");
        if (p.Content == null || p.Content.Length > 10000 || (p.ReferenceCode?.Length ?? 0) > 500) throw new ApiError(400, "Nội dung giao dịch không hợp lệ.");
        var utc = new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Unspecified), TimeSpan.FromHours(7)).UtcDateTime;
        var canonical = JsonSerializer.Serialize(new { p.Id, p.AccountNumber, p.TransferType, Amount = p.TransferAmount.ToString("G29", CultureInfo.InvariantCulture), p.TransactionDate, p.Content, Reference = p.ReferenceCode ?? "" });
        return new(p.Id, p.TransferType == "out" ? -p.TransferAmount : p.TransferAmount, utc, p.Content, p.ReferenceCode ?? "", Hash(canonical));
    }
}

public static class StateRules
{
    public static JsonObject Empty() => new() {
        ["theme"] = "light", ["accentColor"] = "#6366f1", ["monthlyBudget"] = 10000000,
        ["schedules"] = new JsonArray(), ["deadlines"] = new JsonArray(), ["transactions"] = new JsonArray(),
        ["debts"] = new JsonArray(), ["notes"] = new JsonArray(), ["bankSeenIds"] = new JsonArray()
    };
    public static JsonObject Validate(JsonObject state) {
        foreach (var key in new[] { "schedules", "deadlines", "transactions", "debts", "notes" }) {
            if (state[key] is not JsonArray rows) throw new ApiError(400, $"Thiếu danh sách {key}.");
            var ids = new HashSet<string>();
            foreach (var row in rows) {
                if (row is not JsonObject || row["id"] is not JsonValue id || !id.TryGetValue<string>(out var value) || string.IsNullOrWhiteSpace(value) || value.Length > 100 || !ids.Add(value)) throw new ApiError(400, "Bản ghi thiếu ID hoặc có ID trùng.");
            }
        }
        foreach (var tx in (JsonArray)state["transactions"]!) {
            if (tx!["amount"] is not JsonValue amount || !amount.TryGetValue<decimal>(out var value) || value < 0 || value > 9007199254740991m || tx["type"]?.ToString() is not ("income" or "expense")) throw new ApiError(400, "Khoản thu chi không hợp lệ.");
        }
        if (state.ContainsKey("bankSeenIds") && state["bankSeenIds"] is not JsonArray) throw new ApiError(400, "Danh sách đồng bộ không hợp lệ.");
        if (state["bankSeenIds"] is JsonArray seen && seen.Any(id => id is not JsonValue v || !v.TryGetValue<long>(out var n) || n <= 0 || n > 9007199254740991L)) throw new ApiError(400, "Danh sách đồng bộ không hợp lệ.");
        return state;
    }
    public static JsonObject Merge(JsonObject source, IReadOnlyList<BankEvent> events) {
        var result = (JsonObject)source.DeepClone();
        var seen = new HashSet<long>((result["bankSeenIds"] as JsonArray ?? new()).Select(x => x!.GetValue<long>()));
        var ledger = events.ToDictionary(e => "sepay-" + e.Id, e => e);
        var rows = new JsonArray(); var included = new HashSet<string>();
        foreach (var tx in (JsonArray)result["transactions"]!) {
            var id = tx!["id"]!.ToString();
            if (tx["source"]?.ToString() != "sepay" && !id.StartsWith("sepay-", StringComparison.Ordinal)) { rows.Add(tx.DeepClone()); continue; }
            if (!ledger.TryGetValue(id, out var e)) continue;
            seen.Add(e.Id); included.Add(id);
            var canonical = ToTransaction(e);
            foreach (var field in new[] { "title", "category", "note" }) if (tx[field] is JsonValue v && v.TryGetValue<string>(out var text)) canonical[field] = text;
            rows.Add(canonical);
        }
        foreach (var e in events) {
            if (!seen.Contains(e.Id) && !included.Contains("sepay-" + e.Id)) rows.Insert(0, ToTransaction(e));
            seen.Add(e.Id);
        }
        result["transactions"] = rows;
        result["bankSeenIds"] = new JsonArray(seen.Order().Select(id => JsonValue.Create(id)).ToArray<JsonNode?>());
        return result;
    }
    private static JsonObject ToTransaction(BankEvent e) => new() {
        ["id"] = "sepay-" + e.Id, ["source"] = "sepay", ["bankTransactionId"] = e.Id,
        ["type"] = e.Amount < 0 ? "expense" : "income", ["amount"] = Math.Abs(e.Amount),
        ["date"] = e.OccurredAt.AddHours(7).ToString("yyyy-MM-dd"),
        ["title"] = string.IsNullOrWhiteSpace(e.Content) ? (e.Amount < 0 ? "Chi từ VietinBank" : "Thu từ VietinBank") : e.Content,
        ["category"] = e.Amount < 0 ? "other_exp" : "other_inc", ["note"] = "VietinBank · Tự động đồng bộ", ["bankReference"] = e.Reference
    };
}
