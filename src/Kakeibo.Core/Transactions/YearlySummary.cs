namespace Kakeibo.Core.Transactions;

/// <summary>1か月分の収支。月末残高は前年からの繰越を含めた累計。</summary>
public sealed record MonthSummary(int Month, long Income, long Expense, long ClosingBalance)
{
    public long Difference => Income - Expense;
}

/// <summary>1年分の月ごとの収支の推移。</summary>
public sealed record YearlySummary(int Year, long OpeningBalance, IReadOnlyList<MonthSummary> Months)
{
    public long IncomeTotal => Months.Sum(m => m.Income);

    public long ExpenseTotal => Months.Sum(m => m.Expense);

    public long Difference => IncomeTotal - ExpenseTotal;

    /// <param name="year">対象の年。</param>
    /// <param name="openingBalance">前年末までの繰越残高。</param>
    /// <param name="transactions">その年の明細。ほかの年の明細は無視する。</param>
    public static YearlySummary Build(int year, long openingBalance, IEnumerable<Transaction> transactions)
    {
        var byMonth = transactions
            .Where(t => t.Date.Year == year)
            .ToLookup(t => t.Date.Month);

        var months = new List<MonthSummary>(12);
        var balance = openingBalance;
        for (var month = 1; month <= 12; month++)
        {
            var income = byMonth[month].Where(t => t.Kind == TransactionKind.Income).Sum(t => t.Amount);
            var expense = byMonth[month].Where(t => t.Kind == TransactionKind.Expense).Sum(t => t.Amount);
            balance += income - expense;
            months.Add(new MonthSummary(month, income, expense, balance));
        }

        return new YearlySummary(year, openingBalance, months);
    }
}
