using System.Security.Claims;

namespace PersonalFinance.API.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    public Guid Id => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : Guid.Empty;
}
