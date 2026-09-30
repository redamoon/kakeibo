using Kakeibo.Core.Transactions;

namespace Kakeibo.Core.Tests;

public sealed class MonthlyLedgerTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private static Transaction Tx(int day, TransactionKind kind, long amount, int minutes = 0) => new(
        Guid.NewGuid(), "local", new DateOnly(2026, 9, day), kind, amount, Guid.NewGuid(), "",
        BaseTime.AddMinutes(minutes), 1, false);

    [Fact]
    public void Rows_are_oldest_first_with_running_balance()
    {
        var salary = Tx(25, TransactionKind.Income, 250_000);
        var food = Tx(26, TransactionKind.Expense, 3_200);
        var goods = Tx(28, TransactionKind.Expense, 1_480);

        var ledger = MonthlyLedger.Build([goods, salary, food]);

        Assert.Equal([salary, food, goods], ledger.Rows.Select(r => r.Transaction));
        Assert.Equal([250_000L, 246_800L, 245_320L], ledger.Rows.Select(r => r.Balance));
        Assert.Equal(250_000, ledger.IncomeTotal);
        Assert.Equal(4_680, ledger.ExpenseTotal);
        Assert.Equal(245_320, ledger.Difference);
    }

    [Fact]
    public void Same_day_rows_are_ordered_by_record_time()
    {
        var later = Tx(1, TransactionKind.Expense, 100, minutes: 10);
        var earlier = Tx(1, TransactionKind.Expense, 200, minutes: 5);

        var ledger = MonthlyLedger.Build([later, earlier]);

        Assert.Equal([earlier, later], ledger.Rows.Select(r => r.Transaction));
    }

    [Fact]
    public void Balance_can_go_negative()
    {
        var ledger = MonthlyLedger.Build([Tx(1, TransactionKind.Expense, 1_000)]);

        Assert.Equal(-1_000, ledger.Rows.Single().Balance);
        Assert.Equal(-1_000, ledger.Difference);
    }

    [Fact]
    public void Balance_starts_from_opening_balance()
    {
        var ledger = MonthlyLedger.Build([Tx(1, TransactionKind.Expense, 1_000), Tx(2, TransactionKind.Income, 300)], openingBalance: 5_000);

        Assert.Equal([4_000L, 4_300L], ledger.Rows.Select(r => r.Balance));
        Assert.Equal(-700, ledger.Difference);
        Assert.Equal(4_300, ledger.ClosingBalance);
    }

    [Fact]
    public void Empty_month_has_zero_totals()
    {
        var ledger = MonthlyLedger.Build([]);

        Assert.Empty(ledger.Rows);
        Assert.Equal(0, ledger.Difference);
    }
}
