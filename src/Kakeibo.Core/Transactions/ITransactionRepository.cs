namespace Kakeibo.Core.Transactions;

public interface ITransactionRepository
{
    /// <summary>from 以上 to 未満の日付の明細を返す。論理削除済みは含まない。並び順は決めない。</summary>
    Task<IReadOnlyList<Transaction>> GetByPeriodAsync(DateOnly from, DateOnly to);

    /// <summary>指定した日付より前の全明細の、収入の合計から支出の合計を引いた額(繰越残高)。</summary>
    Task<long> GetBalanceBeforeAsync(DateOnly date);

    /// <summary>ID で明細を返す。存在しないか論理削除済みなら null。</summary>
    Task<Transaction?> FindAsync(Guid id);

    Task<Transaction> AddAsync(TransactionDraft draft);

    Task<Transaction> UpdateAsync(Guid id, TransactionDraft draft);

    /// <summary>論理削除する。同期で他端末へ削除を伝えるため、行そのものは残す。</summary>
    Task DeleteAsync(Guid id);
}

public static class TransactionRepositoryExtensions
{
    /// <summary>指定した月の明細を返す。論理削除済みは含まない。並び順は決めない。</summary>
    public static Task<IReadOnlyList<Transaction>> GetByMonthAsync(this ITransactionRepository repository, int year, int month)
    {
        var from = new DateOnly(year, month, 1);
        return repository.GetByPeriodAsync(from, from.AddMonths(1));
    }
}
