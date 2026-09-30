using Kakeibo.Core.Data;
using Kakeibo.Core.Transactions;
using SQLite;

namespace Kakeibo.Core.Tests;

public sealed class SchemaMigrationTests : TestDatabase
{
    public override Task InitializeAsync()
    {
        // 版 0 の DB(明細がカテゴリ名を直接持つ)を作っておく
        using (var db = new SQLiteConnection(Path))
        {
            db.Execute(
                """
                CREATE TABLE transactions (
                    id TEXT PRIMARY KEY, user_id TEXT NOT NULL, date TEXT NOT NULL, kind INTEGER NOT NULL,
                    amount INTEGER NOT NULL, category TEXT NOT NULL, memo TEXT NOT NULL,
                    updated_at INTEGER NOT NULL, version INTEGER NOT NULL, deleted INTEGER NOT NULL)
                """);
            Insert(db, "local", TransactionKind.Expense, "食費");
            Insert(db, "local", TransactionKind.Expense, "食費");
            Insert(db, "local", TransactionKind.Expense, "ペット");
            Insert(db, "local", TransactionKind.Income, "給与");
        }

        return base.InitializeAsync();
    }

    private static void Insert(SQLiteConnection db, string userId, TransactionKind kind, string category) =>
        db.Execute(
            "INSERT INTO transactions VALUES (?, ?, '2026-09-01', ?, 1000, ?, '', 0, 1, 0)",
            Guid.NewGuid().ToString(), userId, (int)kind, category);

    [Fact]
    public async Task Legacy_category_names_are_moved_to_categories_table()
    {
        var categories = await new SqliteCategoryRepository(Database, User, Clock).GetAllAsync();
        var transactions = await new SqliteTransactionRepository(Database, User, Clock).GetByMonthAsync(2026, 9);

        string NameOf(Transaction t) => categories.Single(c => c.Id == t.CategoryId).Name;

        Assert.Equal(["ペット", "給与", "食費", "食費"], transactions.Select(NameOf).Order());

        // 標準のカテゴリも登録され、同じ名前は標準のものに寄せられる
        var expense = categories.Where(c => c.Kind == TransactionKind.Expense).Select(c => c.Name).ToList();
        Assert.Equal("食費", expense.First());
        Assert.Equal("ペット", expense.Last());
        Assert.Single(expense, "食費");
    }

    [Fact]
    public async Task Migration_runs_only_once()
    {
        await new SqliteCategoryRepository(Database, User, Clock).GetAllAsync();
        await Database.DisposeAsync();

        await using var reopened = new KakeiboDatabase(Path);
        var categories = await new SqliteCategoryRepository(reopened, User, Clock).GetAllAsync();

        Assert.Single(categories, c => c.Name == "ペット");
    }
}
