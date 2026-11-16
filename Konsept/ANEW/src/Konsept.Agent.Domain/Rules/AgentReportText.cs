using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Shared.Kernel.Security;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Ajan hata raporunun metin kuralları (spec 0022 Req 4–6, 9): 0008 maskelemesi; Windows kullanıcı adı içeren yollar
/// <c>%USERPROFILE%</c>; makine adı rapora girmez; log satırları yalnızca son 15 dakikanın Warning, Error ve Critical satırları, en fazla
/// 200 (en yeniler); gönderilen ve dosyaya kaydedilen metin aynıdır (<see cref="Compose"/>). Rapor metni geliştiriciye yönelik teknik
/// metindir (arayüz metni değil).
/// </summary>
public static partial class AgentReportText
{
    /// <summary>Rapora eklenen log penceresi.</summary>
    public static readonly TimeSpan LogWindow = TimeSpan.FromMinutes(15);

    /// <summary>Rapora eklenen en fazla log satırı.</summary>
    public const int MaxLogLines = 200;

    /// <summary>Bir log satırının rapordaki en fazla uzunluğu (karakter; fazlası kırpılır).</summary>
    public const int MaxLineLength = 500;

    /// <summary>Rapordaki log satırlarının toplam en fazla uzunluğu (karakter; sunucu sınırıyla aynı, en yeniler kalır).</summary>
    public const int MaxLogChars = 64 * 1024;

    /// <summary>Hata özetinin en fazla uzunluğu (karakter).</summary>
    public const int MaxSummaryLength = 4000;

    /// <summary>Ajanda tutulan en fazla bekleyen bildirim.</summary>
    public const int MaxPending = 20;

    /// <summary>Profil klasörünün yerine yazılan metin.</summary>
    public const string UserProfileToken = "%USERPROFILE%";

    /// <summary>Makine adının yerine yazılan metin.</summary>
    public const string MachineToken = "[bilgisayar]";

    /// <summary>Metni maskeler, kullanıcı profil yollarını kısaltır ve makine adını siler.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="environment">Ortam.</param>
    /// <returns>Temiz metin.</returns>
    public static string Sanitize(string? text, ReportEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var result = text;
        if (!string.IsNullOrWhiteSpace(environment.UserProfile))
        {
            result = result.Replace(environment.UserProfile.TrimEnd('\\', '/'), UserProfileToken, StringComparison.OrdinalIgnoreCase);
        }

        result = UserProfilePath().Replace(result, UserProfileToken);
        if (!string.IsNullOrWhiteSpace(environment.MachineName) && environment.MachineName.Length >= 2)
        {
            result = Regex.Replace(
                result,
                $@"(?<![A-Za-z0-9_-]){Regex.Escape(environment.MachineName)}(?![A-Za-z0-9_-])",
                MachineToken,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1));
        }

        return SecretMasker.Mask(result);
    }

    /// <summary>Rapora girecek log satırlarını seçer: son 15 dakikanın Warning+ satırları, en yeni 200, eskiden yeniye, temiz metin.</summary>
    /// <param name="lines">Yerel log satırları.</param>
    /// <param name="now">Şimdi.</param>
    /// <param name="environment">Ortam.</param>
    /// <returns>Satırlar.</returns>
    public static IReadOnlyList<string> SelectLogLines(IEnumerable<AgentLogLine> lines, DateTimeOffset now, ReportEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var from = now - LogWindow;
        var selected = lines
            .Where(l => l.Level >= AgentLogLevel.Warning && l.At >= from && l.At <= now)
            .OrderBy(l => l.At)
            .TakeLast(MaxLogLines)
            .Select(l => Cut(string.Create(CultureInfo.InvariantCulture, $"{l.At.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ} [{LevelCode(l.Level)}] {Sanitize(l.Text, environment)}"), MaxLineLength))
            .ToList();

        // Toplam sınır: hub mesajı sınırlı kalsın (review r1 H-1); en eski satırlar düşer.
        var total = selected.Sum(l => l.Length);
        var skip = 0;
        while (total > MaxLogChars && skip < selected.Count)
        {
            total -= selected[skip].Length;
            skip++;
        }

        return selected.Skip(skip).ToList();
    }

    /// <summary>Metni sınıra kırpar (sonuna "…" eklenir).</summary>
    /// <param name="text">Metin.</param>
    /// <param name="max">En fazla uzunluk.</param>
    /// <returns>Kırpılmış metin.</returns>
    public static string Cut(string text, int max)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length > max ? text[..(max - 1)] + "…" : text;
    }

    /// <summary>Adımın rapordaki adı.</summary>
    /// <param name="step">Adım.</param>
    /// <returns><c>indirme</c> / <c>doğrulama</c> / <c>kurulum</c>; yoksa <c>null</c>.</returns>
    public static string? StepName(InstallStep? step) => step switch
    {
        InstallStep.Download => "indirme",
        InstallStep.Verify => "doğrulama",
        InstallStep.Install => "kurulum",
        _ => null,
    };

    /// <summary>Gönderilecek (ve dosyaya kaydedilecek) metnin tamamı ("Ayrıntıları göster").</summary>
    /// <param name="report">Rapor.</param>
    /// <returns>Metin.</returns>
    public static string Compose(AgentErrorReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var text = new StringBuilder()
            .AppendLine("ANEW Konsept ajan hata raporu")
            .AppendLine(CultureInfo.InvariantCulture, $"Zaman (UTC): {report.CreatedAt.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}")
            .AppendLine(CultureInfo.InvariantCulture, $"Ajan versiyonu: {report.AgentVersion}")
            .AppendLine(CultureInfo.InvariantCulture, $"İşletim sistemi: {report.OperatingSystem}");
        if (report.Component is not null)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"Bileşen: {report.Component}");
        }

        if (report.Step is not null)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"Adım: {report.Step}");
        }

        text.AppendLine().AppendLine("Hata özeti:").AppendLine(report.Summary).AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Log (son {LogWindow.TotalMinutes:0} dakika, Warning ve üstü, {report.LogLines.Count} satır):");
        foreach (var line in report.LogLines)
        {
            text.AppendLine(line);
        }

        return text.ToString();
    }

    private static string LevelCode(AgentLogLevel level) => level switch
    {
        AgentLogLevel.Warning => "WRN",
        AgentLogLevel.Error => "ERR",
        AgentLogLevel.Critical => "FTL",
        _ => "INF",
    };

    // C:\Users\<ad>\… (ya da C:/Users/<ad>/…): sürücü + Users + kullanıcı adı → %USERPROFILE%.
    [GeneratedRegex(@"[A-Za-z]:[\\/]Users[\\/][^\\/:*?""<>|\s]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UserProfilePath();
}
