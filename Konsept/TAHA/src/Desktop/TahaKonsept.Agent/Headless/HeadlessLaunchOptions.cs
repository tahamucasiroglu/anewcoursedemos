namespace TahaKonsept.Agent.Headless;

/// <summary>
/// Penceresiz geliştirici/test modu seçenekleri (<c>--headless</c>). Avalonia açılmaz; ajan çekirdeği + motorlar çalışır, eşleme kodu
/// dosyaya yazılır (web'den "Kodu gir" ile onaylanır). Örnek:
/// <c>TahaKonsept.Agent.exe --headless --server http://localhost:7191 --pair-code-out C:\tmp\pair.txt</c>.
/// Önemli üyeler: <see cref="Parse"/>, <see cref="IsHeadless"/>.
/// </summary>
/// <param name="ServerUrl">API kök adresi (<c>--server</c>; boşsa kayıtlı/varsayılan).</param>
/// <param name="PairCodeOut">Eşleme kodunun yazılacağı dosya (<c>--pair-code-out</c>; boşsa yalnız log).</param>
/// <param name="SkipPrerequisites">Ön gereksinim kurulumunu atla (<c>--skip-prerequisites</c>).</param>
/// <param name="StopFile">Bu dosya oluşunca düzgün kapan (<c>--stop-file</c>; betiklerden nazik durdurma için).</param>
public sealed record HeadlessLaunchOptions(string? ServerUrl, string? PairCodeOut, bool SkipPrerequisites, string? StopFile = null)
{
    /// <summary>Penceresiz mod bayrağı.</summary>
    public const string HeadlessArgument = "--headless";

    /// <summary>Argümanlarda <c>--headless</c> var mı.</summary>
    /// <param name="args">Komut satırı.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    public static bool IsHeadless(IReadOnlyList<string>? args) =>
        args?.Any(a => string.Equals(a, HeadlessArgument, StringComparison.OrdinalIgnoreCase)) == true;

    /// <summary>Argümanlardan seçenekleri üretir.</summary>
    /// <param name="args">Komut satırı.</param>
    /// <returns>Seçenekler.</returns>
    public static HeadlessLaunchOptions Parse(IReadOnlyList<string>? args)
    {
        var list = args ?? [];
        return new HeadlessLaunchOptions(
            ValueOf(list, "--server")?.TrimEnd('/'),
            ValueOf(list, "--pair-code-out"),
            list.Any(a => string.Equals(a, "--skip-prerequisites", StringComparison.OrdinalIgnoreCase)),
            ValueOf(list, "--stop-file"));
    }

    private static string? ValueOf(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(args[i + 1]))
            {
                return args[i + 1].Trim();
            }
        }

        return null;
    }
}
