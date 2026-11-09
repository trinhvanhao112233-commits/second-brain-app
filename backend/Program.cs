using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using LifeOS;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 2 * 1024 * 1024);
builder.Services.AddSingleton<Store>();
builder.Services.AddSingleton<SessionTokens>();
var protection = builder.Services.AddDataProtection().SetApplicationName("LifeOS");
if (builder.Configuration["DataProtection:KeyPath"] is string keyPath && !string.IsNullOrWhiteSpace(keyPath)) protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? []).AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
app.UseCors();
app.UseRateLimiter();
app.Use(async (context, next) => {
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    try { await next(); }
    catch (ApiError error) { context.Response.StatusCode = error.Status; await context.Response.WriteAsJsonAsync(new { success = false, error = error.Message }); }
    catch (JsonException) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { success = false, error = "JSON không hợp lệ." }); }
    catch (BadHttpRequestException error) { context.Response.StatusCode = error.StatusCode; await context.Response.WriteAsJsonAsync(new { success = false, error = "Yêu cầu không hợp lệ hoặc dữ liệu quá lớn." }); }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
    catch (Exception error) {
        app.Logger.LogError("LifeOS request failed ({ErrorType})", error.GetType().Name);
        context.Response.StatusCode = 500; await context.Response.WriteAsJsonAsync(new { success = false, error = "Không thể lưu dữ liệu. Vui lòng thử lại." });
    }
});
app.MapGet("/api/health", () => Results.Ok(new { ok = true, application = "LifeOS" }));

app.MapPost("/api/auth/register", async (Credentials input, Store db, SessionTokens tokens, CancellationToken ct) => {
    var username = (input.Username ?? "").Trim().ToLowerInvariant(); var name = (input.FullName ?? "").Trim();
    if (username.Length is < 3 or > 50 || !Regex.IsMatch(username, "^[a-z0-9_.-]+$") || name.Length is < 1 or > 100) throw new ApiError(400, "Tên đăng nhập cần 3–50 ký tự không dấu; họ tên cần 1–100 ký tự.");
    CheckPassword(input.Password);
    var user = await db.CreateUser(username, name, input.Password!, ct);
    return Results.Ok(await LoginResult(user, db, tokens, ct));
}).RequireRateLimiting("auth");
app.MapPost("/api/auth/login", async (Credentials input, Store db, SessionTokens tokens, CancellationToken ct) => {
    if ((input.Username?.Length ?? 0) > 50 || string.IsNullOrEmpty(input.Password) || Encoding.UTF8.GetByteCount(input.Password) > 72) throw new ApiError(401, "Tên đăng nhập hoặc mật khẩu không đúng.");
    var user = await db.User((input.Username ?? "").Trim().ToLowerInvariant(), ct);
    if (user == null || !BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash)) throw new ApiError(401, "Tên đăng nhập hoặc mật khẩu không đúng.");
    return Results.Ok(await LoginResult(user, db, tokens, ct));
}).RequireRateLimiting("auth");

var secured = app.MapGroup("/api").AddEndpointFilter(async (invocation, next) => {
    var context = invocation.HttpContext; var header = context.Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) throw new ApiError(401, "Vui lòng đăng nhập.");
    var ticket = context.RequestServices.GetRequiredService<SessionTokens>().Read(header[7..]);
    var user = await context.RequestServices.GetRequiredService<Store>().User(ticket.Id, context.RequestAborted);
    if (user == null || user.PasswordVersion != ticket.Version) throw new ApiError(401, "Phiên đăng nhập đã kết thúc.");
    context.Items["user"] = user; return await next(invocation);
});
secured.MapGet("/session", async (HttpContext context, Store db, CancellationToken ct) => {
    var user = User(context); var snapshot = await db.State(user.Id, ct);
    return Results.Ok(new { user = PublicUser(user), state = snapshot.State, snapshot.Revision, bank = await db.Status(user.Id, ct) });
});
secured.MapPut("/account", async (HttpContext context, AccountInput input, Store db, SessionTokens tokens, CancellationToken ct) => {
    var name = (input.FullName ?? "").Trim(); if (name.Length is < 1 or > 100) throw new ApiError(400, "Họ tên cần 1–100 ký tự.");
    if (!string.IsNullOrEmpty(input.Password)) CheckPassword(input.Password);
    var updated = await db.UpdateUser(User(context), name, input.Password, ct);
    return Results.Ok(new { user = PublicUser(updated), token = tokens.Issue(updated) });
}).RequireRateLimiting("auth");
secured.MapGet("/state", async (HttpContext context, Store db, CancellationToken ct) => {
    var snapshot = await db.State(User(context).Id, ct); return Results.Ok(new { state = snapshot.State, snapshot.Revision, bank = await db.Status(User(context).Id, ct) });
});
secured.MapPut("/state", async (HttpContext context, SaveStateInput input, Store db, CancellationToken ct) => {
    if (input.State == null) throw new ApiError(400, "Thiếu dữ liệu.");
    return Results.Ok(new { revision = await db.Save(User(context).Id, input.State, input.Revision, ct) });
});
secured.MapGet("/bank/status", async (HttpContext context, Store db, CancellationToken ct) => Results.Ok(await db.Status(User(context).Id, ct)));
secured.MapPost("/bank/connect", async (HttpContext context, BankInput input, Store db, CancellationToken ct) => {
    if (input.AccountNumber == null || input.AccountNumber.Length is < 6 or > 30 || input.AccountNumber.Any(c => c is < '0' or > '9')) throw new ApiError(400, "Số tài khoản không hợp lệ.");
    var connection = await db.Connect(User(context).Id, input.AccountNumber, ct); return Results.Ok(new { connection.Id, apiKey = connection.Key, bank = await db.Status(User(context).Id, ct) });
}).RequireRateLimiting("auth");
secured.MapPost("/bank/{id:guid}/rotate-key", async (HttpContext context, Guid id, Store db, CancellationToken ct) => Results.Ok(new { apiKey = await db.Rotate(User(context).Id, id, ct) })).RequireRateLimiting("auth");

app.MapPost("/api/bank/sepay/{id:guid}", async (HttpContext context, Guid id, Store db, CancellationToken ct) => {
    var header = context.Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase) || header.Length != 71) throw new ApiError(401, "Khóa Webhook không hợp lệ.");
    var connection = await db.Connection(id, ct);
    if (connection == null || !BankRules.Authenticate(header, connection.KeyHash)) throw new ApiError(401, "Khóa Webhook không hợp lệ.");
    if (context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) != true) throw new ApiError(415, "Yêu cầu JSON.");
    using var buffer = new MemoryStream(); var chunk = new byte[4096]; int count;
    while ((count = await context.Request.Body.ReadAsync(chunk, ct)) > 0) { if (buffer.Length + count > 65536) throw new ApiError(413, "Dữ liệu Webhook quá lớn."); await buffer.WriteAsync(chunk.AsMemory(0, count), ct); }
    var payload = JsonSerializer.Deserialize<SePayPayload>(buffer.ToArray()) ?? throw new ApiError(400, "Thiếu giao dịch.");
    if (payload.Id == 0) return Results.Ok(new { success = true });
    await db.Receive(connection, BankRules.Validate(payload, connection.AccountNumber), ct);
    return Results.Ok(new { success = true });
});

await app.Services.GetRequiredService<Store>().Initialize();
await app.RunAsync();

static AppUser User(HttpContext context) => (AppUser)context.Items["user"]!;
static object PublicUser(AppUser user) => new { user.Id, user.Username, user.FullName };
static async Task<object> LoginResult(AppUser user, Store db, SessionTokens tokens, CancellationToken ct) {
    var snapshot = await db.State(user.Id, ct); return new { user = PublicUser(user), token = tokens.Issue(user), state = snapshot.State, snapshot.Revision, bank = await db.Status(user.Id, ct) };
}
static void CheckPassword(string? password) { if (string.IsNullOrEmpty(password) || password.Length < 8 || Encoding.UTF8.GetByteCount(password) > 72) throw new ApiError(400, "Mật khẩu cần ít nhất 8 ký tự, tối đa 72 byte UTF-8."); }
public sealed record Credentials(string? Username, string? Password, string? FullName);
public sealed record AccountInput(string? FullName, string? Password);
public sealed record SaveStateInput(JsonObject? State, int Revision);
public sealed record BankInput(string? AccountNumber);
