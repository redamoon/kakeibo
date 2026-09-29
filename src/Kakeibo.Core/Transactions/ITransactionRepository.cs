namespace Kakeibo.Core.Transactions;

public interface ITransactionRepository
{
    /// <summary>指定した月の明細を、日付の新しい順に返す。論理削除済みは含まない。</summary>
    Task<IReadOnlyList<Transaction>> GetByMonthAsync(int year, int month);

    /// <summary>ID で明細を返す。存在しないか論理削除済みなら null。</summary>
    Task<Transaction?> FindAsync(Guid id);

    Task<Transaction> AddAsync(TransactionDraft draft);

    Task<Transaction> UpdateAsync(Guid id, TransactionDraft draft);

    /// <summary>論理削除する。同期で他端末へ削除を伝えるため、行そのものは残す。</summary>
    Task DeleteAsync(Guid id);
}
