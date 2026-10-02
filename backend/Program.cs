using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext dùng MySQL / TiDB Cloud
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));
    options.UseMySql(connectionString, serverVersion);
});


// 2. Cấu hình CORS mở rộng cho Vue Dev Server và Capacitor Mobile App
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Cho phép cả capacitor://, localhost, mobile device IP
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 3. Đăng ký dịch vụ AI Parser (Gemini AI với NLP Fallback)
builder.Services.AddHttpClient<PersonalFinance.API.Services.IAiParserService, PersonalFinance.API.Services.GeminiAiParserService>();

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

// Tự động tạo Database & Tables nếu chưa có
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();

    // Đảm bảo bảng Users và các cột UserId tồn tại
    try
    {
        dbContext.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS `Users` (
                `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
                `Username` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
                `FullName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
                `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
                `CreatedAt` datetime(6) NOT NULL,
                PRIMARY KEY (`Id`),
                UNIQUE KEY `IX_Users_Username` (`Username`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

        dbContext.Database.ExecuteSqlRaw(@"
            ALTER TABLE `Wallets` ADD COLUMN IF NOT EXISTS `UserId` char(36) COLLATE utf8mb4_general_ci NULL;
            ALTER TABLE `Events` ADD COLUMN IF NOT EXISTS `UserId` char(36) COLLATE utf8mb4_general_ci NULL;
            ALTER TABLE `Tasks` ADD COLUMN IF NOT EXISTS `UserId` char(36) COLLATE utf8mb4_general_ci NULL;
            ALTER TABLE `Notes` ADD COLUMN IF NOT EXISTS `UserId` char(36) COLLATE utf8mb4_general_ci NULL;
        ");
    }
    catch (Exception dbEx)
    {
        Console.WriteLine($"[DB MIGRATION NOTE]: {dbEx.Message}");
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n[DATABASE CONNECTION ERROR] Không thể kết nối tới MySQL: {ex.Message}");
    Console.WriteLine("-> Vui lòng kiểm tra mật khẩu trong backend/appsettings.json tại mục MySqlConnection.\n");
    Console.ResetColor();
}

app.Run();

