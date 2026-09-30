using Kakeibo.Desktop.ViewModels;

namespace Kakeibo.Desktop.Views;

public partial class ReportPage : ContentPage
{
    private readonly ReportViewModel _viewModel;

    public ReportPage(ReportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // 明細画面や設定画面での変更を反映する
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
