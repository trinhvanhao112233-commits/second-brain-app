using System.Text.Json;
using MySqlConnector;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Data;
using PersonalFinance.API.Security;
using PersonalFinance.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

// Read-only schema inspection. Never prints credentials or personal records.
using var config = JsonDocument.Parse(await File.ReadAllTextAsync("backend/appsettings.json"));
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MySqlConnection")
    ?? config.RootElement.GetProperty("ConnectionStrings").GetProperty("MySqlConnection").GetString();
var builder = new MySqlConnectionStringBuilder(connectionString ?? throw new InvalidOperationException("Database connection is not configured.")) { ConnectionTimeout = 15, DefaultCommandTimeout = 15 };
try {
    await using var connection = new MySqlConnection(builder.ConnectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'DebtRecords' ORDER BY ORDINAL_POSITION";
    await using (var reader = await command.ExecuteReaderAsync()) {
        var count = 0;
        while (await reader.ReadAsync()) { Console.WriteLine($"{reader.GetString(0)} | {reader.GetString(1)} | nullable={reader.GetString(2)}"); count++; }
        Console.WriteLine($"DebtRecords columns: {count}");
    }
    // Execute the same query shape with an unassigned owner; no personal records are read.
    var userId = Guid.NewGuid();
    var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
    accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "diagnostic"));
    var options = new DbContextOptionsBuilder<AppDbContext>().UseMySql(builder.ConnectionString, new MySqlServerVersion(new Version(8,0,30))).Options;
    await using var db = new AppDbContext(options, new CurrentUser(accessor));
    using var services = new ServiceCollection().AddSingleton(new CurrentUser(accessor)).BuildServiceProvider();
    accessor.HttpContext.RequestServices = services;
    var controller = new DebtsController(db) { ControllerContext = new ControllerContext { HttpContext = accessor.HttpContext } };
    await controller.GetDebts();
    Console.WriteLine("DebtsController.GetDebts: OK (unassigned owner)");
    // A synthetic row exercises materialization without reading or writing user data.
    var sample = await db.DebtRecords.FromSqlInterpolated($@"SELECT
        CAST({Guid.NewGuid()} AS CHAR(36)) AS Id, CAST({userId} AS CHAR(36)) AS UserId,
        'Diagnostic' AS Title, 'DEBT' AS RecordType,
        CAST(1 AS DECIMAL(18,2)) AS TotalAmount, CAST(0 AS DECIMAL(18,2)) AS PaidAmount,
        NULL AS BreakdownNote, CURRENT_TIMESTAMP AS DueDate, '1-10' AS PeriodLabel,
        1 AS PriorityLevel, FALSE AS IsCompleted, NULL AS CategoryTag, UTC_TIMESTAMP AS CreatedAt")
        .AsNoTracking().SingleAsync();
    if (sample.UserId != userId) throw new InvalidOperationException("Synthetic Guid materialization failed.");
    Console.WriteLine("Synthetic CHAR(36) row materialization: OK");
} catch (MySqlException error) {
    Console.Error.WriteLine($"Database diagnostic failed: MySqlException {error.Number}");
    if (error.Number is 1054 or 1146 or 1064 or 1267) Console.Error.WriteLine(error.Message);
    Environment.ExitCode = 1;
}
