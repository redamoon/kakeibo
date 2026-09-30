namespace Kakeibo.Core.Transactions;

/// <summary>帳簿の1行。その行までの月内の累計残高を持つ。</summary>
public sealed record LedgerRow(Transaction Transaction, long Balance);

/// <summary>
/// 1か月分の明細を、通帳のように古い順に並べて累計残高をつけたもの。
/// 残高は月初を 0 とした、その月の収支の累計。
/// </summary>
public sealed record MonthlyLedger(IReadOnlyList<LedgerRow> Rows, long IncomeTotal, long ExpenseTotal)
{
    public long Difference => IncomeTotal - ExpenseTotal;

    public static MonthlyLedger Build(IEnumerable<Transaction> transactions)
    {
        var rows = new List<LedgerRow>();
        long income = 0;
        long expense = 0;

        // 同じ日付の中では、先に記録したものを上にする
        foreach (var transaction in transactions.OrderBy(t => t.Date).ThenBy(t => t.UpdatedAt))
        {
            if (transaction.Kind == TransactionKind.Income)
            {
                income += transaction.Amount;
            }
            else
            {
                expense += transaction.Amount;
            }

            rows.Add(new LedgerRow(transaction, income - expense));
        }

        return new MonthlyLedger(rows, income, expense);
    }
}
