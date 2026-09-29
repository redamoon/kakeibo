using Kakeibo.Desktop.ViewModels;

namespace Kakeibo.Desktop.Views;

public partial class TransactionListPage : ContentPage
{
    private readonly TransactionListViewModel _viewModel;

    public TransactionListPage(TransactionListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // 編集画面から戻ったときにも最新の内容を表示する
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
