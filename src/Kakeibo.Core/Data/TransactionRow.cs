using Kakeibo.Core.Transactions;
using SQLite;

namespace Kakeibo.Core.Data;

/// <summary>
/// transactions テーブルの行。sqlite-net が扱える型だけで表す。
/// </summary>
[Table("transactions")]
internal sealed class TransactionRow
{
    private const string DateFormat = "yyyy-MM-dd";

    [PrimaryKey, Column("id")]
    public string Id { get; set; } = "";

    [Column("user_id"), NotNull, Indexed(Name = "ix_transactions_user_date", Order = 1)]
    public string UserId { get; set; } = "";

    /// <summary>yyyy-MM-dd 形式。文字列の大小比較で期間検索できる。</summary>
    [Column("date"), NotNull, Indexed(Name = "ix_transactions_user_date", Order = 2)]
    public string Date { get; set; } = "";

    [Column("kind"), NotNull]
    public int Kind { get; set; }

    [Column("amount"), NotNull]
    public long Amount { get; set; }

    [Column("category"), NotNull]
    public string Category { get; set; } = "";

    [Column("memo"), NotNull]
    public string Memo { get; set; } = "";

    /// <summary>UTC の Unix 時刻(ミリ秒)。Last Write Wins の比較に使う。</summary>
    [Column("updated_at"), NotNull]
    public long UpdatedAt { get; set; }

    [Column("version"), NotNull]
    public long Version { get; set; }

    [Column("deleted"), NotNull]
    public bool Deleted { get; set; }

    public static string FormatDate(DateOnly date) => date.ToString(DateFormat, System.Globalization.CultureInfo.InvariantCulture);

    public Transaction ToModel() => new(
        Guid.Parse(Id),
        UserId,
        DateOnly.ParseExact(Date, DateFormat, System.Globalization.CultureInfo.InvariantCulture),
        (TransactionKind)Kind,
        Amount,
        Category,
        Memo,
        DateTimeOffset.FromUnixTimeMilliseconds(UpdatedAt),
        Version,
        Deleted);

    public void Apply(TransactionDraft draft)
    {
        Date = FormatDate(draft.Date);
        Kind = (int)draft.Kind;
        Amount = draft.Amount;
        Category = draft.Category.Trim();
        Memo = draft.Memo.Trim();
    }
}
