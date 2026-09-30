using SQLite;

namespace Kakeibo.Core.Data;

/// <summary>
/// ローカル SQLite への接続。初回アクセス時にスキーマを最新にする。
/// スキーマの版は PRAGMA user_version で管理する。
/// </summary>
public sealed class KakeiboDatabase : IAsyncDisposable
{
    /// <summary>
    /// 1: カテゴリを categories テーブルに分け、明細は category_id で参照する。
    /// </summary>
    private const int CurrentSchemaVersion = 1;

    private readonly SQLiteAsyncConnection _connection;
    private readonly Lazy<Task> _initialize;

    public KakeiboDatabase(string databasePath)
    {
        _connection = new SQLiteAsyncConnection(
            databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        _initialize = new Lazy<Task>(InitializeAsync);
    }

    internal async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        await _initialize.Value;
        return _connection;
    }

    public async ValueTask DisposeAsync() => await _connection.CloseAsync();

    private async Task InitializeAsync()
    {
        var version = await _connection.ExecuteScalarAsync<int>("PRAGMA user_version");

        await _connection.CreateTableAsync<CategoryRow>();
        if (version < 1)
        {
            await _connection.RunInTransactionAsync(MigrateCategoryNamesToTable);
        }

        // 新規の DB ではここでテーブルが作られる
        await _connection.CreateTableAsync<TransactionRow>();

        if (version < CurrentSchemaVersion)
        {
            await _connection.ExecuteAsync($"PRAGMA user_version = {CurrentSchemaVersion}");
        }
    }

    /// <summary>
    /// 版 0 → 1。明細にカテゴリ名を直接持っていた DB を、categories テーブルの参照に移す。
    /// 利用者ごとに標準のカテゴリを登録したうえで、同じ種類・同じ名前があればそれに寄せ、
    /// なければ標準の後ろにカテゴリを追加する。
    /// </summary>
    private static void MigrateCategoryNamesToTable(SQLiteConnection db)
    {
        var columns = db.GetTableInfo("transactions");
        if (!columns.Any(c => c.Name == "category"))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        db.Execute("ALTER TABLE transactions ADD COLUMN category_id TEXT NOT NULL DEFAULT ''");

        var legacy = db.Query<LegacyCategory>(
            "SELECT DISTINCT user_id AS UserId, kind AS Kind, category AS Name FROM transactions ORDER BY user_id, kind, category");
        foreach (var userId in legacy.Select(c => c.UserId).Distinct())
        {
            var categories = CategoryRow.CreateDefaults(userId, now);

            foreach (var old in legacy.Where(c => c.UserId == userId))
            {
                var name = old.Name.Trim();
                var category = categories.FirstOrDefault(c => c.Kind == old.Kind && c.Name == name);
                if (category is null)
                {
                    category = new CategoryRow
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = userId,
                        Kind = old.Kind,
                        Name = name.Length == 0 ? "未分類" : name,
                        SortOrder = categories.Where(c => c.Kind == old.Kind).Max(c => c.SortOrder) + 1,
                        UpdatedAt = now,
                        Version = 1,
                    };
                    categories.Add(category);
                }

                db.Execute(
                    "UPDATE transactions SET category_id = ? WHERE user_id = ? AND kind = ? AND category = ?",
                    category.Id, userId, old.Kind, old.Name);
            }

            db.InsertAll(categories, runInTransaction: false);
        }

        db.Execute("ALTER TABLE transactions DROP COLUMN category");
    }

    private sealed class LegacyCategory
    {
        public string UserId { get; set; } = "";

        public int Kind { get; set; }

        public string Name { get; set; } = "";
    }
}
