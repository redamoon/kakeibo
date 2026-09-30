using Kakeibo.Desktop.ViewModels;

namespace Kakeibo.Desktop.Views;

public partial class MonthlyTransactionsPage : ContentPage
{
    private readonly MonthlyTransactionsViewModel _viewModel;

    public MonthlyTransactionsPage(MonthlyTransactionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        // 追加・更新のあとは金額欄にカーソルを戻し、続けて入力できるようにする
        _viewModel.EntryReset += (_, _) => AmountEntry.Focus();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
        AmountEntry.Focus();
    }
}
