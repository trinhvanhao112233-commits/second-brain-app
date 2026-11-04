using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using PersonalFinance.API.DTOs;

namespace PersonalFinance.API.Security;

public sealed record AiProposal(Guid UserId, DateTimeOffset ExpiresAt, AiActionOutput Action);
public sealed class AiProposals(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("PersonalOS.AI.Proposal.v1");
    public string Issue(Guid userId, AiActionOutput action) => protector.Protect(JsonSerializer.Serialize(new AiProposal(userId, DateTimeOffset.UtcNow.AddMinutes(10), action)));
    public AiProposal Read(string token) => JsonSerializer.Deserialize<AiProposal>(protector.Unprotect(token))!;
}
