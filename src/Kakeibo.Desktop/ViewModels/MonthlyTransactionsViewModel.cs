using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kakeibo.Core.Transactions;

namespace Kakeibo.Desktop.ViewModels;

/// <summary>
/// 月ごとの明細一覧と、その上の入力欄をまとめて扱う。
/// 一覧で明細を選ぶと入力欄が編集モードになり、選択を外すと追加モードに戻る。
/// </summary>
public sealed partial class MonthlyTransactionsViewModel : ObservableObject
{
    private readonly ITransactionRepository _repository;
    private readonly TimeProvider _timeProvider;
    private DateOnly _month;

    public MonthlyTransactionsViewModel(ITransactionRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _month = FirstDayOf(Today());
        Date = DefaultDate();
    }

    /// <summary>追加・更新のあと、続けて入力できるように画面側でフォーカスを戻すための通知。</summary>
    public event EventHandler? EntryReset;

    public ObservableCollection<TransactionItemViewModel> Items { get; } = [];

    public string MonthText => _month.ToString("yyyy年M月");

    [ObservableProperty]
    public partial string IncomeTotalText { get; set; } = "0";

    [ObservableProperty]
    public partial string ExpenseTotalText { get; set; } = "0";

    [ObservableProperty]
    public partial string DifferenceText { get; set; } = "0";

    [ObservableProperty]
    public partial bool IsDifferenceNegative { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    public partial TransactionItemViewModel? SelectedItem { get; set; }

    public bool IsEditing => SelectedItem is not null;

    [ObservableProperty]
    public partial DateTime Date { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncome))]
    public partial bool IsExpense { get; set; } = true;

    public bool IsIncome
    {
        get => !IsExpense;
        set => IsExpense = !value;
    }

    [ObservableProperty]
    public partial string AmountText { get; set; } = "";

    [ObservableProperty]
    public partial string Category { get; set; } = "";

    [ObservableProperty]
    public partial string Memo { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => ErrorMessage is not null;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var transactions = await _repository.GetByMonthAsync(_month.Year, _month.Month);
        var ledger = MonthlyLedger.Build(transactions);

        Items.Clear();
        foreach (var row in ledger.Rows)
        {
            Items.Add(new TransactionItemViewModel(row));
        }

        IncomeTotalText = Money.Format(ledger.IncomeTotal);
        ExpenseTotalText = Money.Format(ledger.ExpenseTotal);
        DifferenceText = Money.FormatSigned(ledger.Difference);
        IsDifferenceNegative = ledger.Difference < 0;
    }

    [RelayCommand]
    private Task PreviousMonthAsync() => ShowMonthAsync(_month.AddMonths(-1));

    [RelayCommand]
    private Task NextMonthAsync() => ShowMonthAsync(_month.AddMonths(1));

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!long.TryParse(AmountText.Replace(",", ""), NumberStyles.None, CultureInfo.InvariantCulture, out var amount))
        {
            ErrorMessage = "金額は半角数字で入力してください。";
            return;
        }

        var draft = new TransactionDraft(
            DateOnly.FromDateTime(Date),
            IsExpense ? TransactionKind.Expense : TransactionKind.Income,
            amount,
            Category,
            Memo);

        try
        {
            if (SelectedItem is { } selected)
            {
                await _repository.UpdateAsync(selected.Id, draft);
            }
            else
            {
                await _repository.AddAsync(draft);
            }
        }
        catch (ArgumentException ex)
        {
            // ArgumentException.Message の末尾に付く " (Parameter 'xxx')" は画面に出さない
            ErrorMessage = ex.Message.Split(" (Parameter")[0];
            return;
        }

        // 表示中と別の月の日付で登録したときは、その月へ移動して結果が見えるようにする
        var savedMonth = FirstDayOf(draft.Date);
        if (savedMonth != _month)
        {
            _month = savedMonth;
            OnPropertyChanged(nameof(MonthText));
        }

        await LoadAsync();
        ResetEntry(keepDateAndKind: true);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedItem is not { } selected)
        {
            return;
        }

        var confirmed = await Shell.Current.DisplayAlertAsync("明細の削除", "この明細を削除しますか?", "削除", "キャンセル");
        if (!confirmed)
        {
            return;
        }

        await _repository.DeleteAsync(selected.Id);
        await LoadAsync();
        ResetEntry(keepDateAndKind: false);
    }

    [RelayCommand]
    private void CancelEdit() => ResetEntry(keepDateAndKind: false);

    partial void OnSelectedItemChanged(TransactionItemViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        var transaction = value.Transaction;
        Date = transaction.Date.ToDateTime(TimeOnly.MinValue);
        IsExpense = transaction.Kind == TransactionKind.Expense;
        AmountText = transaction.Amount.ToString(CultureInfo.InvariantCulture);
        Category = transaction.Category;
        Memo = transaction.Memo;
        ErrorMessage = null;
    }

    private async Task ShowMonthAsync(DateOnly month)
    {
        _month = month;
        OnPropertyChanged(nameof(MonthText));
        await LoadAsync();
        ResetEntry(keepDateAndKind: false);
    }

    /// <summary>入力欄を追加モードの空の状態に戻す。</summary>
    private void ResetEntry(bool keepDateAndKind)
    {
        SelectedItem = null;
        if (!keepDateAndKind)
        {
            Date = DefaultDate();
            IsExpense = true;
        }

        AmountText = "";
        Category = "";
        Memo = "";
        ErrorMessage = null;
        EntryReset?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>表示中の月が今月なら今日、それ以外はその月の1日。</summary>
    private DateTime DefaultDate()
    {
        var today = Today();
        var date = FirstDayOf(today) == _month ? today : _month;
        return date.ToDateTime(TimeOnly.MinValue);
    }

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    private static DateOnly FirstDayOf(DateOnly date) => new(date.Year, date.Month, 1);
}
