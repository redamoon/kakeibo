using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kakeibo.Core.Transactions;
using Kakeibo.Desktop.Views;

namespace Kakeibo.Desktop.ViewModels;

public sealed partial class TransactionListViewModel(
    ITransactionRepository repository,
    TimeProvider timeProvider) : ObservableObject
{
    private DateOnly _month = FirstDayOf(DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime));

    public ObservableCollection<TransactionItemViewModel> Items { get; } = [];

    public string MonthText => _month.ToString("yyyy年M月");

    [RelayCommand]
    private async Task LoadAsync()
    {
        var transactions = await repository.GetByMonthAsync(_month.Year, _month.Month);

        Items.Clear();
        foreach (var transaction in transactions)
        {
            Items.Add(new TransactionItemViewModel(transaction));
        }
    }

    [RelayCommand]
    private Task PreviousMonthAsync() => MoveMonthAsync(-1);

    [RelayCommand]
    private Task NextMonthAsync() => MoveMonthAsync(1);

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(TransactionEditPage));

    [RelayCommand]
    private Task EditAsync(TransactionItemViewModel item) =>
        Shell.Current.GoToAsync(nameof(TransactionEditPage), new ShellNavigationQueryParameters
        {
            [TransactionEditViewModel.IdQueryKey] = item.Id,
        });

    private async Task MoveMonthAsync(int months)
    {
        _month = _month.AddMonths(months);
        OnPropertyChanged(nameof(MonthText));
        await LoadAsync();
    }

    private static DateOnly FirstDayOf(DateOnly date) => new(date.Year, date.Month, 1);
}
