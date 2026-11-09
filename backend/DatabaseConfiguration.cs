using MySqlConnector;

namespace LifeOS;

public sealed class DatabaseConfigurationException(string message) : Exception(message);

public static class DatabaseConfiguration
{
    public static string Read(IConfiguration configuration)
    {
        var value = configuration.GetConnectionString("MySqlConnection");
        if (string.IsNullOrWhiteSpace(value)) throw new DatabaseConfigurationException(
            "LIFEOS_DATABASE_CONFIG_MISSING: Set ConnectionStrings__MySqlConnection in Render > Environment, then Save, rebuild, and deploy. Do not put database passwords in source code.");
        MySqlConnectionStringBuilder options;
        try { options = new MySqlConnectionStringBuilder(value); }
        catch (ArgumentException) { throw new DatabaseConfigurationException(
            "LIFEOS_DATABASE_CONFIG_INVALID: ConnectionStrings__MySqlConnection must be a MySQL connection string: Server=HOST;Port=PORT;Database=NAME;User ID=USER;Password=SECRET; with the TLS options required by your database provider. Its value has not been logged."); }
        if (!options.ContainsKey("Server") || string.IsNullOrWhiteSpace(options.Server) || string.IsNullOrWhiteSpace(options.Database) || string.IsNullOrWhiteSpace(options.UserID)) throw new DatabaseConfigurationException(
            "LIFEOS_DATABASE_CONFIG_INCOMPLETE: ConnectionStrings__MySqlConnection must explicitly specify Server, Database and User ID. Use the connection details from your MySQL/TiDB provider. Its value has not been logged.");
        return options.ConnectionString;
    }

    public static string StartupHint(int number) => number switch {
        1045 => "Database authentication failed. Check User ID and Password in Render Environment; if you rotated the database password, update it there too.",
        1049 => "The configured database does not exist. Check Database in ConnectionStrings__MySqlConnection.",
        1044 or 1142 => "The database user lacks access. Check database permissions; LifeOS needs CREATE TABLE for its new tables and read/write access to them.",
        _ => "Could not open or initialize MySQL. Check Server, Port, TLS options, database availability and network access rules. The connection string has not been logged."
    };
}
