namespace Kakeibo.Core.Transactions;

/// <summary>カテゴリ1つ分の合計と、全体に占める割合(0〜1)。</summary>
public sealed record CategoryTotal(Guid CategoryId, long Amount, double Ratio);

/// <summary>支出または収入の、カテゴリごとの内訳。金額の大きい順。</summary>
public sealed record CategoryBreakdown(TransactionKind Kind, long Total, IReadOnlyList<CategoryTotal> Items)
{
    public static CategoryBreakdown Build(IEnumerable<Transaction> transactions, TransactionKind kind)
    {
        var sums = transactions
            .Where(t => t.Kind == kind)
            .GroupBy(t => t.CategoryId)
            .Select(g => (CategoryId: g.Key, Amount: g.Sum(t => t.Amount)))
            .ToList();
        var total = sums.Sum(s => s.Amount);

        var items = sums
            .OrderByDescending(s => s.Amount)
            .Select(s => new CategoryTotal(s.CategoryId, s.Amount, total == 0 ? 0 : (double)s.Amount / total))
            .ToList();

        return new CategoryBreakdown(kind, total, items);
    }
}
