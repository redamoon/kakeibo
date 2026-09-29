using SQLite;

namespace Kakeibo.Core.Data;

/// <summary>
/// ローカル SQLite への接続。初回アクセス時にテーブルを作成する。
/// </summary>
public sealed class KakeiboDatabase : IAsyncDisposable
{
    private readonly SQLiteAsyncConnection _connection;
    private readonly Lazy<Task> _initialize;

    public KakeiboDatabase(string databasePath)
    {
        _connection = new SQLiteAsyncConnection(
            databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        _initialize = new Lazy<Task>(() => _connection.CreateTableAsync<TransactionRow>());
    }

    internal async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        await _initialize.Value;
        return _connection;
    }

    public async ValueTask DisposeAsync() => await _connection.CloseAsync();
}
