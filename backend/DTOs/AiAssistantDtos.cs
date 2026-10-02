using System;
using System.Text.Json.Serialization;

namespace PersonalFinance.API.DTOs
{
    public record AiProcessRequest(
        string Message,
        Guid? DefaultWalletId = null
    );

    public class AiActionOutput
    {
        [JsonPropertyName("actionType")]
        public string ActionType { get; set; } = "chat"; // "add_expense", "add_income", "add_event", "add_task", "chat"

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("walletName")]
        public string? WalletName { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("startTime")]
        public string? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public string? EndTime { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("replyMessage")]
        public string ReplyMessage { get; set; } = string.Empty;
    }

    public record AiProcessResponse(
        bool Success,
        string Reply,
        string? ActionExecuted, // e.g. "transaction", "event", "task", "none"
        object? Data
    );
}
