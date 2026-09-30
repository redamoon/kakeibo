using Kakeibo.Core.Transactions;

namespace Kakeibo.Desktop.ViewModels;

/// <summary>帳簿の1行分の表示内容。</summary>
public sealed class TransactionItemViewModel(LedgerRow row, string categoryName)
{
    public Transaction Transaction => row.Transaction;

    public Guid Id => row.Transaction.Id;

    public string DateText => row.Transaction.Date.ToString("M/d (ddd)");

    public string Category => categoryName;

    public string Memo => row.Transaction.Memo;

    public bool HasMemo => !string.IsNullOrEmpty(row.Transaction.Memo);

    public bool IsIncome => row.Transaction.Kind == TransactionKind.Income;

    /// <summary>収入の列に出す金額。支出の行では空。</summary>
    public string IncomeText => IsIncome ? Money.Format(row.Transaction.Amount) : "";

    /// <summary>支出の列に出す金額。収入の行では空。</summary>
    public string ExpenseText => IsIncome ? "" : Money.Format(row.Transaction.Amount);

    public string BalanceText => Money.Format(row.Balance);

    public bool IsBalanceNegative => row.Balance < 0;
}
