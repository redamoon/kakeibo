using Kakeibo.Core.Categories;
using Kakeibo.Core.Transactions;
using SQLite;

namespace Kakeibo.Core.Data;

/// <summary>categories テーブルの行。</summary>
[Table("categories")]
internal sealed class CategoryRow
{
    [PrimaryKey, Column("id")]
    public string Id { get; set; } = "";

    [Column("user_id"), NotNull, Indexed(Name = "ix_categories_user", Order = 1)]
    public string UserId { get; set; } = "";

    [Column("kind"), NotNull]
    public int Kind { get; set; }

    [Column("name"), NotNull]
    public string Name { get; set; } = "";

    [Column("sort_order"), NotNull]
    public int SortOrder { get; set; }

    /// <summary>UTC の Unix 時刻(ミリ秒)。</summary>
    [Column("updated_at"), NotNull]
    public long UpdatedAt { get; set; }

    [Column("version"), NotNull]
    public long Version { get; set; }

    [Column("deleted"), NotNull]
    public bool Deleted { get; set; }

    /// <summary>標準のカテゴリの行を作る。並び順は種類ごとに 0 から振る。</summary>
    public static List<CategoryRow> CreateDefaults(string userId, long updatedAt) =>
        DefaultCategories.All
            .GroupBy(c => c.Kind)
            .SelectMany(group => group.Select((c, index) => new CategoryRow
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Kind = (int)c.Kind,
                Name = c.Name,
                SortOrder = index,
                UpdatedAt = updatedAt,
                Version = 1,
            }))
            .ToList();

    public Category ToModel() => new(
        Guid.Parse(Id),
        UserId,
        (TransactionKind)Kind,
        Name,
        SortOrder,
        DateTimeOffset.FromUnixTimeMilliseconds(UpdatedAt),
        Version,
        Deleted);
}
