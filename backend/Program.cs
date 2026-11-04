using System;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext dùng MySQL / TiDB Cloud kèm Auto-Retry chống rớt kết nối tạm thời
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

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
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 3. Đăng ký dịch vụ AI Parser & Web Push Notification
builder.Services.AddHttpClient<PersonalFinance.API.Services.IAiParserService, PersonalFinance.API.Services.GeminiAiParserService>();
builder.Services.AddScoped<PersonalFinance.API.Services.INotificationService, PersonalFinance.API.Services.WebPushNotificationService>();
builder.Services.AddHostedService<PersonalFinance.API.Services.EventReminderBackgroundWorker>();

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
app.UseAuthorization();
app.MapControllers();

// 4. Tự động khởi tạo từng bảng độc lập trên TiDB Cloud (Mỗi lệnh tách riêng try/catch để không chặn nhau)
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try { dbContext.Database.EnsureCreated(); } catch { }

    string[] ddlStatements = new[]
    {
        @"CREATE TABLE IF NOT EXISTS `Users` (
            `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
            `Username` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
            `FullName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
            `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
            `CreatedAt` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`),
            UNIQUE KEY `IX_Users_Username` (`Username`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS `DebtRecords` (
            `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
            `UserId` char(36) COLLATE utf8mb4_general_ci NULL,
            `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
            `RecordType` varchar(30) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'DEBT',
            `TotalAmount` decimal(18,2) NOT NULL DEFAULT 0,
            `PaidAmount` decimal(18,2) NOT NULL DEFAULT 0,
            `BreakdownNote` varchar(500) CHARACTER SET utf8mb4 NULL,
            `DueDate` datetime(6) NOT NULL,
            `PeriodLabel` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT '1-10',
            `PriorityLevel` int NOT NULL DEFAULT 2,
            `IsCompleted` tinyint(1) NOT NULL DEFAULT 0,
            `CategoryTag` varchar(100) CHARACTER SET utf8mb4 NULL,
            `CreatedAt` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS `PushSubscriptions` (
            `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
            `UserId` char(36) COLLATE utf8mb4_general_ci NULL,
            `Endpoint` text COLLATE utf8mb4_general_ci NOT NULL,
            `P256dh` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
            `Auth` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
            `CreatedAt` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"ALTER TABLE `Wallets` ADD COLUMN `UserId` char(36) COLLATE utf8mb4_general_ci NULL;",
        @"ALTER TABLE `Wallets` ADD COLUMN `IsMaster` tinyint(1) NOT NULL DEFAULT 0;",
        @"ALTER TABLE `Events` ADD COLUMN `UserId` char(36) COLLATE utf8mb4_general_ci NULL;",
        @"ALTER TABLE `Events` ADD COLUMN `IsReminderSent` tinyint(1) NOT NULL DEFAULT 0;",
        @"ALTER TABLE `Tasks` ADD COLUMN `UserId` char(36) COLLATE utf8mb4_general_ci NULL;",
        @"ALTER TABLE `Notes` ADD COLUMN `UserId` char(36) COLLATE utf8mb4_general_ci NULL;"
    };

    foreach (var sql in ddlStatements)
    {
        try
        {
            dbContext.Database.ExecuteSqlRaw(sql);
        }
        catch
        {
            // Bỏ qua nếu cột hoặc bảng đã tồn tại
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[DB Startup Warning]: {ex.Message}");
}

app.Run();
