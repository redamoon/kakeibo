using System.Globalization;

namespace Kakeibo.Desktop.ViewModels;

internal static class Money
{
    private static readonly CultureInfo Japanese = CultureInfo.GetCultureInfo("ja-JP");

    /// <summary>3桁区切りの円表示。単位は列見出しで示すため付けない。</summary>
    public static string Format(long amount) => amount.ToString("N0", Japanese);

    /// <summary>差額用。プラスにも符号を付ける。</summary>
    public static string FormatSigned(long amount) => amount > 0 ? $"+{Format(amount)}" : Format(amount);
}
