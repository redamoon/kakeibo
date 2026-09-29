namespace Kakeibo.Core.Transactions;

/// <summary>
/// 家計簿の明細。同期用のメタデータ(UserId / UpdatedAt / Version / Deleted)を含む。
/// </summary>
public sealed record Transaction(
    Guid Id,
    string UserId,
    DateOnly Date,
    TransactionKind Kind,
    long Amount,
    string Category,
    string Memo,
    DateTimeOffset UpdatedAt,
    long Version,
    bool Deleted);

/// <summary>
/// 画面から入力される明細の内容。ID や同期用のメタデータはリポジトリ側で決める。
/// </summary>
public sealed record TransactionDraft(
    DateOnly Date,
    TransactionKind Kind,
    long Amount,
    string Category,
    string Memo)
{
    public void Validate()
    {
        if (Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Amount), Amount, "金額は1円以上で入力してください。");
        }

        if (string.IsNullOrWhiteSpace(Category))
        {
            throw new ArgumentException("カテゴリを入力してください。", nameof(Category));
        }
    }
}
