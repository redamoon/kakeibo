using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kakeibo.Core.Transactions;

namespace Kakeibo.Desktop.ViewModels;

public sealed partial class TransactionEditViewModel(
    ITransactionRepository repository,
    TimeProvider timeProvider) : ObservableObject, IQueryAttributable
{
    public const string IdQueryKey = "id";

    private Guid? _id;

    public string Title => IsEditing ? "明細の編集" : "明細の追加";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial DateTime Date { get; set; } = timeProvider.GetLocalNow().Date;

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

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(IdQueryKey, out var value) || value is not Guid id)
        {
            return;
        }

        var transaction = await repository.FindAsync(id);
        if (transaction is null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        _id = id;
        IsEditing = true;
        Date = transaction.Date.ToDateTime(TimeOnly.MinValue);
        IsExpense = transaction.Kind == TransactionKind.Expense;
        AmountText = transaction.Amount.ToString(CultureInfo.InvariantCulture);
        Category = transaction.Category;
        Memo = transaction.Memo;
    }

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
            if (_id is { } id)
            {
                await repository.UpdateAsync(id, draft);
            }
            else
            {
                await repository.AddAsync(draft);
            }
        }
        catch (ArgumentException ex)
        {
            // ArgumentException.Message の末尾に付く " (Parameter 'xxx')" は画面に出さない
            ErrorMessage = ex.Message.Split(" (Parameter")[0];
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_id is not { } id)
        {
            return;
        }

        var confirmed = await Shell.Current.DisplayAlertAsync("明細の削除", "この明細を削除しますか?", "削除", "キャンセル");
        if (!confirmed)
        {
            return;
        }

        await repository.DeleteAsync(id);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
