using Kakeibo.Core.Transactions;

namespace Kakeibo.Core.Tests;

public sealed class SummaryTests
{
    private static readonly Guid Food = Guid.NewGuid();
    private static readonly Guid Goods = Guid.NewGuid();
    private static readonly Guid Salary = Guid.NewGuid();

    private static Transaction Tx(int year, int month, TransactionKind kind, long amount, Guid? category = null) => new(
        Guid.NewGuid(), "local", new DateOnly(year, month, 1), kind, amount, category ?? Food, "",
        DateTimeOffset.UnixEpoch, 1, false);

    [Fact]
    public void Yearly_summary_has_twelve_months_with_running_balance()
    {
        var summary = YearlySummary.Build(2026, openingBalance: 10_000,
        [
            Tx(2026, 1, TransactionKind.Income, 200_000),
            Tx(2026, 1, TransactionKind.Expense, 150_000),
            Tx(2026, 3, TransactionKind.Expense, 70_000),
            Tx(2025, 12, TransactionKind.Income, 999_999), // 別の年は無視する
        ]);

        Assert.Equal(12, summary.Months.Count);
        Assert.Equal(new MonthSummary(1, 200_000, 150_000, 60_000), summary.Months[0]);
        Assert.Equal(new MonthSummary(2, 0, 0, 60_000), summary.Months[1]);
        Assert.Equal(new MonthSummary(3, 0, 70_000, -10_000), summary.Months[2]);
        Assert.Equal(-10_000, summary.Months[11].ClosingBalance);
        Assert.Equal(200_000, summary.IncomeTotal);
        Assert.Equal(220_000, summary.ExpenseTotal);
        Assert.Equal(-20_000, summary.Difference);
    }

    [Fact]
    public void Category_breakdown_is_sorted_by_amount_with_ratio()
    {
        var breakdown = CategoryBreakdown.Build(
        [
            Tx(2026, 9, TransactionKind.Expense, 1_000, Goods),
            Tx(2026, 9, TransactionKind.Expense, 2_000, Food),
            Tx(2026, 9, TransactionKind.Expense, 1_000, Food),
            Tx(2026, 9, TransactionKind.Income, 250_000, Salary),
        ], TransactionKind.Expense);

        Assert.Equal(4_000, breakdown.Total);
        Assert.Equal(
            [new CategoryTotal(Food, 3_000, 0.75), new CategoryTotal(Goods, 1_000, 0.25)],
            breakdown.Items);
    }

    [Fact]
    public void Empty_breakdown_has_no_items()
    {
        var breakdown = CategoryBreakdown.Build([Tx(2026, 9, TransactionKind.Expense, 100)], TransactionKind.Income);

        Assert.Equal(0, breakdown.Total);
        Assert.Empty(breakdown.Items);
    }
}
