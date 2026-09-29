using Kakeibo.Core.Transactions;

namespace Kakeibo.Desktop.ViewModels;

/// <summary>一覧の1行分の表示内容。</summary>
public sealed class TransactionItemViewModel(Transaction transaction)
{
    public Guid Id => transaction.Id;

    public string DateText => transaction.Date.ToString("M/d (ddd)");

    public string Category => transaction.Category;

    public string Memo => transaction.Memo;

    public bool HasMemo => !string.IsNullOrEmpty(transaction.Memo);

    public bool IsIncome => transaction.Kind == TransactionKind.Income;

    public string AmountText => $"{(IsIncome ? "+" : "-")}{transaction.Amount:N0}円";
}
