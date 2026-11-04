using Microsoft.AspNetCore.Mvc;

namespace PersonalFinance.API.Security;

public abstract class OwnedController : ControllerBase
{
    protected Guid? userId => HttpContext.RequestServices.GetRequiredService<CurrentUser>().Id;
}
