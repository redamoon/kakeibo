using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kakeibo.Core.Categories;
using Kakeibo.Core.Transactions;

namespace Kakeibo.Desktop.ViewModels;

/// <summary>
/// 集計画面。1年分の月ごとの推移と、選んだ月のカテゴリ別内訳を表示する。
/// </summary>
public sealed partial class ReportViewModel : ObservableObject
{
    private readonly ITransactionRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly TimeProvider _timeProvider;
    private int _year;
    private IReadOnlyList<Transaction> _yearTransactions = [];
    private Dictionary<Guid, Category> _categories = [];

    public ReportViewModel(ITransactionRepository repository, ICategoryRepository categoryRepository, TimeProvider timeProvider)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _timeProvider = timeProvider;
        _year = Today().Year;
    }

    public string YearText => $"{_year}年";

    public ObservableCollection<MonthSummaryItemViewModel> Months { get; } = [];

    [ObservableProperty]
    public partial string OpeningBalanceText { get; set; } = "0";

    [ObservableProperty]
    public partial string IncomeTotalText { get; set; } = "0";

    [ObservableProperty]
    public partial string ExpenseTotalText { get; set; } = "0";

    [ObservableProperty]
    public partial string DifferenceText { get; set; } = "0";

    [ObservableProperty]
    public partial bool IsDifferenceNegative { get; set; }

    [ObservableProperty]
    public partial MonthSummaryItemViewModel? SelectedMonth { get; set; }

    /// <summary>内訳を支出で見るか(false なら収入)。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncomeBreakdown))]
    public partial bool IsExpenseBreakdown { get; set; } = true;

    public bool IsIncomeBreakdown
    {
        get => !IsExpenseBreakdown;
        set => IsExpenseBreakdown = !value;
    }

    public ObservableCollection<CategoryTotalItemViewModel> Breakdown { get; } = [];

    [ObservableProperty]
    public partial string BreakdownTitle { get; set; } = "";

    [ObservableProperty]
    public partial bool IsBreakdownEmpty { get; set; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        _categories = categories.ToDictionary(c => c.Id);

        var from = new DateOnly(_year, 1, 1);
        _yearTransactions = await _repository.GetByPeriodAsync(from, from.AddYears(1));
        var openingBalance = await _repository.GetBalanceBeforeAsync(from);
        var summary = YearlySummary.Build(_year, openingBalance, _yearTransactions);

        var today = Today();
        var selectedMonth = SelectedMonth?.Month ?? (today.Year == _year ? today.Month : 1);

        Months.Clear();
        foreach (var month in summary.Months)
        {
            var isFuture = new DateOnly(_year, month.Month, 1) > today;
            Months.Add(new MonthSummaryItemViewModel(month, isFuture));
        }

        OpeningBalanceText = Money.Format(summary.OpeningBalance);
        IncomeTotalText = Money.Format(summary.IncomeTotal);
        ExpenseTotalText = Money.Format(summary.ExpenseTotal);
        DifferenceText = Money.FormatSigned(summary.Difference);
        IsDifferenceNegative = summary.Difference < 0;

        SelectMonth(Months[selectedMonth - 1]);
    }

    [RelayCommand]
    private Task PreviousYearAsync() => ShowYearAsync(_year - 1);

    [RelayCommand]
    private Task NextYearAsync() => ShowYearAsync(_year + 1);

    [RelayCommand]
    private void SelectMonth(MonthSummaryItemViewModel month)
    {
        if (SelectedMonth is not null)
        {
            SelectedMonth.IsSelected = false;
        }

        month.IsSelected = true;
        SelectedMonth = month;
        RefreshBreakdown();
    }

    partial void OnIsExpenseBreakdownChanged(bool value) => RefreshBreakdown();

    private async Task ShowYearAsync(int year)
    {
        _year = year;
        SelectedMonth = null;
        OnPropertyChanged(nameof(YearText));
        await LoadAsync();
    }

    private void RefreshBreakdown()
    {
        if (SelectedMonth is not { } month)
        {
            return;
        }

        var kind = IsExpenseBreakdown ? TransactionKind.Expense : TransactionKind.Income;
        var breakdown = CategoryBreakdown.Build(_yearTransactions.Where(t => t.Date.Month == month.Month), kind);

        Breakdown.Clear();
        foreach (var item in breakdown.Items)
        {
            var name = _categories.TryGetValue(item.CategoryId, out var category) ? category.Name : "(不明なカテゴリ)";
            Breakdown.Add(new CategoryTotalItemViewModel(name, item.Amount, item.Ratio));
        }

        BreakdownTitle = $"{month.MonthText}の{(IsExpenseBreakdown ? "支出" : "収入")}の内訳　合計 {Money.Format(breakdown.Total)}";
        IsBreakdownEmpty = Breakdown.Count == 0;
    }

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
}
