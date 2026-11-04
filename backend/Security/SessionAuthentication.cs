using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;
using PersonalFinance.API.Models;

namespace PersonalFinance.API.Security;

public sealed record SessionTicket(Guid UserId, string PasswordHash, DateTimeOffset ExpiresAt);

public sealed class SessionTokens(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("PersonalOS.Session.v1");
    public string Issue(User user) => protector.Protect(JsonSerializer.Serialize(
        new SessionTicket(user.Id, user.PasswordHash, DateTimeOffset.UtcNow.AddDays(7))));
    public SessionTicket Read(string token) => JsonSerializer.Deserialize<SessionTicket>(protector.Unprotect(token))!;
}

public sealed class SessionAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, SessionTokens tokens, AppDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        SessionTicket ticket;
        try { ticket = tokens.Read(header[7..]); }
        catch { return AuthenticateResult.Fail("Phiên đăng nhập không hợp lệ."); }
        if (ticket.ExpiresAt <= DateTimeOffset.UtcNow) return AuthenticateResult.Fail("Phiên đăng nhập đã hết hạn.");
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ticket.UserId);
        if (user == null || user.PasswordHash != ticket.PasswordHash) return AuthenticateResult.Fail("Phiên đăng nhập đã kết thúc.");
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
