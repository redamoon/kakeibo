using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kakeibo.Core.Categories;
using Kakeibo.Core.Transactions;

namespace Kakeibo.Desktop.ViewModels;

/// <summary>設定画面のカテゴリ管理。支出用・収入用のカテゴリを並べて編集する。</summary>
public sealed partial class CategorySettingsViewModel(ICategoryRepository repository) : ObservableObject
{
    public ObservableCollection<Category> ExpenseCategories { get; } = [];

    public ObservableCollection<Category> IncomeCategories { get; } = [];

    [ObservableProperty]
    public partial string NewExpenseName { get; set; } = "";

    [ObservableProperty]
    public partial string NewIncomeName { get; set; } = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        var active = (await repository.GetAllAsync()).Where(c => !c.Deleted).ToList();
        Replace(ExpenseCategories, active.Where(c => c.Kind == TransactionKind.Expense));
        Replace(IncomeCategories, active.Where(c => c.Kind == TransactionKind.Income));
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        if (await TryAsync(() => repository.AddAsync(TransactionKind.Expense, NewExpenseName)))
        {
            NewExpenseName = "";
        }
    }

    [RelayCommand]
    private async Task AddIncomeAsync()
    {
        if (await TryAsync(() => repository.AddAsync(TransactionKind.Income, NewIncomeName)))
        {
            NewIncomeName = "";
        }
    }

    [RelayCommand]
    private async Task RenameAsync(Category category)
    {
        var name = await Shell.Current.DisplayPromptAsync(
            "カテゴリ名の変更", "新しい名前を入力してください。過去の明細の表示も変わります。",
            accept: "変更", cancel: "キャンセル", initialValue: category.Name);
        if (name is null || name.Trim() == category.Name)
        {
            return;
        }

        await TryAsync(() => repository.RenameAsync(category.Id, name));
    }

    [RelayCommand]
    private Task MoveUpAsync(Category category) => TryAsync(() => repository.MoveAsync(category.Id, -1));

    [RelayCommand]
    private Task MoveDownAsync(Category category) => TryAsync(() => repository.MoveAsync(category.Id, 1));

    [RelayCommand]
    private async Task DeleteAsync(Category category)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "カテゴリの削除",
            $"「{category.Name}」を削除しますか?\n入力の選択肢からは消えますが、登録済みの明細にはこのカテゴリ名が残ります。",
            "削除", "キャンセル");
        if (confirmed)
        {
            await TryAsync(() => repository.DeleteAsync(category.Id));
        }
    }

    /// <summary>操作を実行して一覧を読み直す。入力の誤りはダイアログで知らせる。</summary>
    private async Task<bool> TryAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (ArgumentException ex)
        {
            // ArgumentException.Message の末尾に付く " (Parameter 'xxx')" は画面に出さない
            await Shell.Current.DisplayAlertAsync("カテゴリ", ex.Message.Split(" (Parameter")[0], "OK");
            return false;
        }
        finally
        {
            await LoadAsync();
        }
    }

    private static void Replace(ObservableCollection<Category> target, IEnumerable<Category> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
