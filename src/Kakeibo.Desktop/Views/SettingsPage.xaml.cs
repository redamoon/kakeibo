using Kakeibo.Desktop.ViewModels;

namespace Kakeibo.Desktop.Views;

public partial class SettingsPage : ContentPage
{
    private readonly CategorySettingsViewModel _viewModel;

    public SettingsPage(CategorySettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
