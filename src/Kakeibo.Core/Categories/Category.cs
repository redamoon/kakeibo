using Kakeibo.Core.Transactions;

namespace Kakeibo.Core.Categories;

/// <summary>
/// 明細のカテゴリ。支出用と収入用に分かれる。同期用のメタデータを含む。
/// 論理削除されたカテゴリも、過去の明細の表示に使うため残す。
/// </summary>
public sealed record Category(
    Guid Id,
    string UserId,
    TransactionKind Kind,
    string Name,
    int SortOrder,
    DateTimeOffset UpdatedAt,
    long Version,
    bool Deleted);
