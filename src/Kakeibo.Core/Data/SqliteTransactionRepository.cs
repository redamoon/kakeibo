using Kakeibo.Core.Accounts;
using Kakeibo.Core.Transactions;

namespace Kakeibo.Core.Data;

public sealed class SqliteTransactionRepository(
    KakeiboDatabase database,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetByPeriodAsync(DateOnly from, DateOnly to)
    {
        var connection = await database.GetConnectionAsync();
        // sqlite-net の LINQ は文字列の大小比較を変換できないため、SQL で書く
        var rows = await connection.QueryAsync<TransactionRow>(
            """
            SELECT * FROM transactions
            WHERE user_id = ? AND deleted = 0 AND date >= ? AND date < ?
            """,
            currentUser.UserId, TransactionRow.FormatDate(from), TransactionRow.FormatDate(to));

        return rows.Select(r => r.ToModel()).ToList();
    }

    public async Task<long> GetBalanceBeforeAsync(DateOnly date)
    {
        var connection = await database.GetConnectionAsync();
        return await connection.ExecuteScalarAsync<long>(
            """
            SELECT COALESCE(SUM(CASE WHEN kind = ? THEN amount ELSE -amount END), 0) FROM transactions
            WHERE user_id = ? AND deleted = 0 AND date < ?
            """,
            (int)TransactionKind.Income, currentUser.UserId, TransactionRow.FormatDate(date));
    }

    public async Task<Transaction?> FindAsync(Guid id)
    {
        var row = await FindRowAsync(id);
        return row?.ToModel();
    }

    public async Task<Transaction> AddAsync(TransactionDraft draft)
    {
        await ValidateAsync(draft);

        var row = new TransactionRow
        {
            Id = Guid.NewGuid().ToString(),
            UserId = currentUser.UserId,
            UpdatedAt = Now(),
            Version = 1,
        };
        row.Apply(draft);

        var connection = await database.GetConnectionAsync();
        await connection.InsertAsync(row);
        return row.ToModel();
    }

    public async Task<Transaction> UpdateAsync(Guid id, TransactionDraft draft)
    {
        await ValidateAsync(draft);

        var row = await FindRowAsync(id) ?? throw new KeyNotFoundException($"明細 {id} が見つかりません。");
        row.Apply(draft);
        Touch(row);

        var connection = await database.GetConnectionAsync();
        await connection.UpdateAsync(row);
        return row.ToModel();
    }

    public async Task DeleteAsync(Guid id)
    {
        var row = await FindRowAsync(id);
        if (row is null)
        {
            return;
        }

        row.Deleted = true;
        Touch(row);

        var connection = await database.GetConnectionAsync();
        await connection.UpdateAsync(row);
    }

    /// <summary>
    /// 入力内容を検証する。カテゴリは論理削除済みでもよい(削除前に登録した明細を編集できるように)が、
    /// 利用者本人のもので、明細と同じ種類(支出/収入)である必要がある。
    /// </summary>
    private async Task ValidateAsync(TransactionDraft draft)
    {
        draft.Validate();

        var key = draft.CategoryId.ToString();
        var userId = currentUser.UserId;
        var connection = await database.GetConnectionAsync();
        var category = await connection.Table<CategoryRow>()
            .Where(r => r.Id == key && r.UserId == userId)
            .FirstOrDefaultAsync();

        if (category is null || category.Kind != (int)draft.Kind)
        {
            throw new ArgumentException("カテゴリを選択し直してください。", nameof(draft));
        }
    }

    private async Task<TransactionRow?> FindRowAsync(Guid id)
    {
        var key = id.ToString();
        var userId = currentUser.UserId;

        var connection = await database.GetConnectionAsync();
        return await connection.Table<TransactionRow>()
            .Where(r => r.Id == key && r.UserId == userId && !r.Deleted)
            .FirstOrDefaultAsync();
    }

    private void Touch(TransactionRow row)
    {
        row.UpdatedAt = Now();
        row.Version++;
    }

    private long Now() => timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
}
