using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using PersonalFinance.API.Security;
using System;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;

if (args.Contains("--generate-vapid")) {
    var keys = WebPush.VapidHelper.GenerateVapidKeys();
    Console.WriteLine($"VAPID_PUBLIC_KEY={keys.PublicKey}");
    Console.WriteLine($"VAPID_PRIVATE_KEY={keys.PrivateKey}");
    return;
}

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext dùng MySQL / TiDB Cloud kèm Auto-Retry chống rớt kết nối tạm thời
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddSingleton<SessionTokens>();
builder.Services.AddSingleton<AiProposals>();
var protection = builder.Services.AddDataProtection().SetApplicationName("PersonalOS");
var keyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(keyPath)) protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddAuthentication("Session").AddScheme<AuthenticationSchemeOptions, SessionAuthentication>("Session", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

// 2. Cấu hình CORS cho phép Web (Vercel, Localhost) và Mobile App (Capacitor/PWA)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? new[] { "https://second-brain-app-sepia.vercel.app", "http://localhost:5173", "http://localhost", "capacitor://localhost" };
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 3. Đăng ký dịch vụ AI Parser & Web Push Notification
builder.Services.AddHttpClient<PersonalFinance.API.Services.IAiParserService, PersonalFinance.API.Services.GeminiAiParserService>();
builder.Services.AddScoped<PersonalFinance.API.Services.INotificationService, PersonalFinance.API.Services.WebPushNotificationService>();
builder.Services.AddHostedService<PersonalFinance.API.Services.EventReminderBackgroundWorker>();

builder.Services.AddScoped<PersonalFinance.API.Services.ReminderDispatcher>();
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.AddPolicy("sensitive", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Run versioned, additive schema upgrade before accepting requests.
if (builder.Configuration.GetValue("Database:AutoUpgrade", true)) {
    using var scope = app.Services.CreateScope();
    await PersonalFinance.API.Data.SchemaUpgrade.Apply(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}
app.Run();
