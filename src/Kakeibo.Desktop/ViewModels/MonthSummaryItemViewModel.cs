using CommunityToolkit.Mvvm.ComponentModel;
using Kakeibo.Core.Transactions;

namespace Kakeibo.Desktop.ViewModels;

/// <summary>年間推移の表の1行(1か月分)。まだ来ていない月は数字を出さない。</summary>
public sealed partial class MonthSummaryItemViewModel(MonthSummary summary, bool isFuture) : ObservableObject
{
    private const string Blank = "-";

    public int Month => summary.Month;

    public string MonthText => $"{summary.Month}月";

    public string IncomeText => isFuture ? Blank : Money.Format(summary.Income);

    public string ExpenseText => isFuture ? Blank : Money.Format(summary.Expense);

    public string DifferenceText => isFuture ? Blank : Money.FormatSigned(summary.Difference);

    public bool IsDifferenceNegative => !isFuture && summary.Difference < 0;

    public string BalanceText => isFuture ? Blank : Money.Format(summary.ClosingBalance);

    public bool IsBalanceNegative => !isFuture && summary.ClosingBalance < 0;

    /// <summary>下のカテゴリ別内訳に表示している月か。</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
