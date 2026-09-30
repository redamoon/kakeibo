using Kakeibo.Core.Transactions;

namespace Kakeibo.Core.Categories;

/// <summary>初回起動時に登録する標準のカテゴリ。</summary>
public static class DefaultCategories
{
    public static IReadOnlyList<(TransactionKind Kind, string Name)> All { get; } =
    [
        (TransactionKind.Expense, "食費"),
        (TransactionKind.Expense, "日用品"),
        (TransactionKind.Expense, "住居費"),
        (TransactionKind.Expense, "水道光熱費"),
        (TransactionKind.Expense, "通信費"),
        (TransactionKind.Expense, "交通費"),
        (TransactionKind.Expense, "医療費"),
        (TransactionKind.Expense, "趣味・娯楽"),
        (TransactionKind.Expense, "交際費"),
        (TransactionKind.Expense, "その他"),
        (TransactionKind.Income, "給与"),
        (TransactionKind.Income, "賞与"),
        (TransactionKind.Income, "その他"),
    ];
}
