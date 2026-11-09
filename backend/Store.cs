using System.Text.Json.Nodes;
using MySqlConnector;

namespace LifeOS;

public sealed class Store(IConfiguration config)
{
    private async Task<MySqlConnection> Open(CancellationToken ct = default) {
        var connection = new MySqlConnection(DatabaseConfiguration.Read(config));
        try { await connection.OpenAsync(ct); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
    private static MySqlCommand Cmd(MySqlConnection db, string sql, params object?[] values) {
        var command = new MySqlCommand(sql, db);
        for (var i = 0; i < values.Length; i++) command.Parameters.AddWithValue("@p" + i, values[i]);
        return command;
    }
    public async Task Initialize() {
        await using var db = await Open();
        foreach (var sql in Schema.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) { await using var command = new MySqlCommand(sql, db); await command.ExecuteNonQueryAsync(); }
    }
    public async Task<AppUser?> User(string username, CancellationToken ct) {
        await using var db = await Open(ct); await using var command = Cmd(db, "SELECT Id,Username,FullName,PasswordHash,PasswordVersion FROM LifeOsUsers WHERE Username=@p0", username);
        await using var reader = await command.ExecuteReaderAsync(ct); return await reader.ReadAsync(ct) ? ReadUser(reader) : null;
    }
    public async Task<AppUser?> User(Guid id, CancellationToken ct) {
        await using var db = await Open(ct); await using var command = Cmd(db, "SELECT Id,Username,FullName,PasswordHash,PasswordVersion FROM LifeOsUsers WHERE Id=@p0", id);
        await using var reader = await command.ExecuteReaderAsync(ct); return await reader.ReadAsync(ct) ? ReadUser(reader) : null;
    }
    private static AppUser ReadUser(MySqlDataReader r) => new(r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4));
    public async Task<AppUser> CreateUser(string username, string name, string password, CancellationToken ct) {
        var user = new AppUser(Guid.NewGuid(), username, name, BCrypt.Net.BCrypt.HashPassword(password), 0);
        await using var db = await Open(ct); await using var tx = await db.BeginTransactionAsync(ct);
        try {
            await using var command = Cmd(db, "INSERT INTO LifeOsUsers (Id,Username,FullName,PasswordHash,PasswordVersion) VALUES (@p0,@p1,@p2,@p3,0)", user.Id, username, name, user.PasswordHash); command.Transaction = tx; await command.ExecuteNonQueryAsync(ct);
            await using var state = Cmd(db, "INSERT INTO LifeOsStates (UserId,StateJson,Revision) VALUES (@p0,@p1,0)", user.Id, StateRules.Empty().ToJsonString()); state.Transaction = tx; await state.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct); return user;
        } catch (MySqlException e) when (e.Number == 1062) { throw new ApiError(409, "Tên đăng nhập đã tồn tại."); }
    }
    public async Task<AppUser> UpdateUser(AppUser user, string name, string? password, CancellationToken ct) {
        var hash = string.IsNullOrEmpty(password) ? user.PasswordHash : BCrypt.Net.BCrypt.HashPassword(password);
        await using var db = await Open(ct); await using var command = Cmd(db, "UPDATE LifeOsUsers SET FullName=@p0,PasswordHash=@p1,PasswordVersion=PasswordVersion+1 WHERE Id=@p2 AND PasswordVersion=@p3", name, hash, user.Id, user.PasswordVersion);
        if (await command.ExecuteNonQueryAsync(ct) != 1) throw new ApiError(409, "Thông tin tài khoản đã thay đổi. Hãy đăng nhập lại.");
        return user with { FullName = name, PasswordHash = hash, PasswordVersion = user.PasswordVersion + 1 };
    }
    public async Task<StateRow> State(Guid owner, CancellationToken ct) {
        await using var db = await Open(ct); await using var command = Cmd(db, "SELECT StateJson,Revision FROM LifeOsStates WHERE UserId=@p0", owner);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new ApiError(404, "Không tìm thấy dữ liệu tài khoản.");
        var state = (JsonObject)JsonNode.Parse(reader.GetString(0))!; var revision = reader.GetInt32(1); await reader.DisposeAsync();
        return new(StateRules.Merge(state, await Events(owner, ct)), revision);
    }
    public async Task<int> Save(Guid owner, JsonObject state, int revision, CancellationToken ct) {
        if (revision < 0) throw new ApiError(400, "Phiên bản dữ liệu không hợp lệ.");
        var merged = StateRules.Merge(StateRules.Validate(state), await Events(owner, ct));
        await using var db = await Open(ct); await using var command = Cmd(db, "UPDATE LifeOsStates SET StateJson=@p0,Revision=Revision+1 WHERE UserId=@p1 AND Revision=@p2", merged.ToJsonString(), owner, revision);
        if (await command.ExecuteNonQueryAsync(ct) != 1) throw new ApiError(409, "Dữ liệu đã thay đổi ở cửa sổ khác. Hãy tải bản sao lưu rồi tải lại.");
        return revision + 1;
    }
    public async Task<BankConnection?> Connection(Guid id, CancellationToken ct) {
        await using var db = await Open(ct); await using var command = Cmd(db, "SELECT Id,UserId,AccountNumber,KeyHash FROM LifeOsBankConnections WHERE Id=@p0", id);
        await using var reader = await command.ExecuteReaderAsync(ct); return await reader.ReadAsync(ct) ? new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3)) : null;
    }
    public async Task<BankStatus> Status(Guid owner, CancellationToken ct) {
        await using var db = await Open(ct); await using var command = Cmd(db, "SELECT c.Id,c.AccountNumber,COUNT(e.SePayId),MAX(e.ReceivedAt) FROM LifeOsBankConnections c LEFT JOIN LifeOsBankEvents e ON e.ConnectionId=c.Id WHERE c.UserId=@p0 GROUP BY c.Id,c.AccountNumber", owner);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(reader.GetGuid(0), reader.GetString(1), reader.GetInt64(2), reader.IsDBNull(3) ? null : DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc)) : new(null, "", 0, null);
    }
    public async Task<(Guid Id, string Key)> Connect(Guid owner, string account, CancellationToken ct) {
        var id = Guid.NewGuid(); var key = BankRules.NewKey();
        await using var db = await Open(ct); await using var command = Cmd(db, "INSERT INTO LifeOsBankConnections (Id,UserId,AccountNumber,KeyHash) VALUES (@p0,@p1,@p2,@p3)", id, owner, account, BankRules.Hash(key));
        try { await command.ExecuteNonQueryAsync(ct); }
        catch (MySqlException e) when (e.Number == 1062) { throw new ApiError(409, "Tài khoản của bạn đã có kết nối. Hãy tải lại hoặc tạo khóa mới nếu đã quên."); }
        return (id, key);
    }
    public async Task<string> Rotate(Guid owner, Guid connection, CancellationToken ct) {
        var key = BankRules.NewKey(); await using var db = await Open(ct);
        await using var command = Cmd(db, "UPDATE LifeOsBankConnections SET KeyHash=@p0 WHERE Id=@p1 AND UserId=@p2", BankRules.Hash(key), connection, owner);
        if (await command.ExecuteNonQueryAsync(ct) != 1) throw new ApiError(404, "Không tìm thấy kết nối."); return key;
    }
    public async Task Receive(BankConnection connection, BankEvent e, CancellationToken ct) {
        await using var db = await Open(ct);
        // The immutable bank event IS the ledger entry. It is committed before
        // acknowledgement; application state is a projection of this ledger.
        await using var command = Cmd(db, "INSERT INTO LifeOsBankEvents (ConnectionId,SePayId,SignedAmount,OccurredAt,Content,ReferenceCode,PayloadHash) VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6)", connection.Id, e.Id, e.Amount, e.OccurredAt, e.Content, e.Reference, e.Hash);
        try { await command.ExecuteNonQueryAsync(ct); }
        catch (MySqlException duplicate) when (duplicate.Number == 1062) {
            await using var check = Cmd(db, "SELECT PayloadHash FROM LifeOsBankEvents WHERE ConnectionId=@p0 AND SePayId=@p1", connection.Id, e.Id);
            if (await check.ExecuteScalarAsync(ct) is not string stored || stored != e.Hash) throw new ApiError(409, "ID giao dịch đã tồn tại với nội dung khác.");
        }
    }
    public async Task<List<BankEvent>> Events(Guid owner, CancellationToken ct) {
        await using var db = await Open(ct); await using var command = Cmd(db, "SELECT e.SePayId,e.SignedAmount,e.OccurredAt,e.Content,e.ReferenceCode,e.PayloadHash FROM LifeOsBankEvents e JOIN LifeOsBankConnections c ON e.ConnectionId=c.Id WHERE c.UserId=@p0 ORDER BY e.SePayId", owner);
        await using var reader = await command.ExecuteReaderAsync(ct); var rows = new List<BankEvent>();
        while (await reader.ReadAsync(ct)) rows.Add(new(reader.GetInt64(0), reader.GetDecimal(1), DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc), reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        return rows;
    }
    public const string Schema = """
        CREATE TABLE IF NOT EXISTS LifeOsUsers (
          Id char(36) NOT NULL PRIMARY KEY, Username varchar(50) NOT NULL UNIQUE,
          FullName varchar(100) NOT NULL, PasswordHash varchar(200) NOT NULL,
          PasswordVersion int NOT NULL DEFAULT 0
        ) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
        CREATE TABLE IF NOT EXISTS LifeOsStates (
          UserId char(36) NOT NULL PRIMARY KEY, StateJson longtext NOT NULL, Revision int NOT NULL DEFAULT 0,
          FOREIGN KEY (UserId) REFERENCES LifeOsUsers(Id)
        ) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
        CREATE TABLE IF NOT EXISTS LifeOsBankConnections (
          Id char(36) NOT NULL PRIMARY KEY, UserId char(36) NOT NULL UNIQUE,
          AccountNumber varchar(30) NOT NULL, KeyHash varchar(64) NOT NULL,
          FOREIGN KEY (UserId) REFERENCES LifeOsUsers(Id)
        ) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
        CREATE TABLE IF NOT EXISTS LifeOsBankEvents (
          ConnectionId char(36) NOT NULL, SePayId bigint NOT NULL,
          SignedAmount decimal(18,2) NOT NULL, OccurredAt datetime(6) NOT NULL,
          Content longtext NOT NULL, ReferenceCode varchar(500) NOT NULL, PayloadHash varchar(64) NOT NULL,
          ReceivedAt datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
          PRIMARY KEY (ConnectionId,SePayId), FOREIGN KEY (ConnectionId) REFERENCES LifeOsBankConnections(Id)
        ) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
        """;
}
