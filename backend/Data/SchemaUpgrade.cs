using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace PersonalFinance.API.Data;

public static class SchemaUpgrade
{
    public static async Task Apply(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS AppSchemaVersions (Version varchar(32) PRIMARY KEY, AppliedAt datetime(6) NOT NULL)");
        var applied = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM AppSchemaVersions WHERE Version = '20261105'").SingleAsync();
        if (applied > 0) return;
        var assembly = typeof(SchemaUpgrade).Assembly;
        using var stream = assembly.GetManifestResourceStream("PersonalFinance.API.Schema.20261105.sql") ?? throw new InvalidOperationException("Missing schema upgrade resource.");
        using var reader = new StreamReader(stream);
        foreach (var statement in (await reader.ReadToEndAsync()).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            try { await db.Database.ExecuteSqlRawAsync(statement); }
            catch (MySqlException error) when (error.Number is 1060 or 1061) { /* Existing column/index on legacy deployments. */ }
        }
        await db.Database.ExecuteSqlRawAsync("INSERT IGNORE INTO AppSchemaVersions (Version, AppliedAt) VALUES ('20261105', UTC_TIMESTAMP(6))");
    }
}
