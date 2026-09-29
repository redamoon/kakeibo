using Kakeibo.Desktop.ViewModels;

namespace Kakeibo.Desktop.Views;

public partial class TransactionEditPage : ContentPage
{
    public TransactionEditPage(TransactionEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
