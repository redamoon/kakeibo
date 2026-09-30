namespace Kakeibo.Desktop.ViewModels;

/// <summary>カテゴリ別内訳の1行。</summary>
public sealed class CategoryTotalItemViewModel(string name, long amount, double ratio)
{
    public string Name => name;

    public string AmountText => Money.Format(amount);

    /// <summary>棒の長さ(0〜1)。</summary>
    public double Ratio => ratio;

    public string PercentText => $"{ratio:P0}";
}
