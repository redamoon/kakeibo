namespace Kakeibo.Core.Transactions;

/// <summary>帳簿の1行。その行までの累計残高を持つ。</summary>
public sealed record LedgerRow(Transaction Transaction, long Balance);

/// <summary>
/// 1か月分の明細を、通帳のように古い順に並べて累計残高をつけたもの。
/// 残高は前月からの繰越(<see cref="OpeningBalance"/>)に、その月の収支を積み上げた額。
/// </summary>
public sealed record MonthlyLedger(IReadOnlyList<LedgerRow> Rows, long OpeningBalance, long IncomeTotal, long ExpenseTotal)
{
    public long Difference => IncomeTotal - ExpenseTotal;

    public long ClosingBalance => OpeningBalance + Difference;

    public static MonthlyLedger Build(IEnumerable<Transaction> transactions, long openingBalance = 0)
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

            rows.Add(new LedgerRow(transaction, openingBalance + income - expense));
        }

        return new MonthlyLedger(rows, openingBalance, income, expense);
    }
}
