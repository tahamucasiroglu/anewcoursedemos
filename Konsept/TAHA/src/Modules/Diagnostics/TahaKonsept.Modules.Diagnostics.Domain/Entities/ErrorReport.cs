using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Domain.Errors;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Domain.Entities;

/// <summary>
/// Kullanıcının (Web), ajanın ya da kurulum ekranının gönderdiği hata bildirimi (tablo <c>diagnostics.error_reports</c>).
/// Önemli üyeler: <see cref="Create"/>, <see cref="ChangeStatus"/> (durum akışı), <see cref="CanTransition"/>.
/// Akış: New → Seen, New → Resolved, Seen → Resolved, Resolved → New (yeniden aç); aynı duruma geçiş etkisizdir.
/// </summary>
public sealed class ErrorReport : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>Mesajın en büyük uzunluğu.</summary>
    public const int MaxMessageLength = 2000;

    /// <summary>Ayrıntının en büyük uzunluğu.</summary>
    public const int MaxDetailsLength = 32000;

    /// <summary>Sayfa adresinin en büyük uzunluğu.</summary>
    public const int MaxPageUrlLength = 1000;

    /// <summary>Tarayıcı bilgisinin en büyük uzunluğu.</summary>
    public const int MaxUserAgentLength = 512;

    /// <summary>Sürüm metninin en büyük uzunluğu.</summary>
    public const int MaxAppVersionLength = 64;

    /// <summary>Makine adının en büyük uzunluğu.</summary>
    public const int MaxMachineLength = 128;

    private ErrorReport()
    {
    }

    /// <summary>Bildiren kullanıcı (oturumsuzsa <see cref="Guid.Empty"/>).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Geldiği yer.</summary>
    public ErrorReportSource Source { get; private set; }

    /// <summary>Kısa açıklama.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Ayrıntı / yığın izi.</summary>
    public string? Details { get; private set; }

    /// <summary>Sayfa adresi.</summary>
    public string? PageUrl { get; private set; }

    /// <summary>Tarayıcı bilgisi.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>Uygulama sürümü.</summary>
    public string? AppVersion { get; private set; }

    /// <summary>Makine adı.</summary>
    public string? Machine { get; private set; }

    /// <summary>Durum.</summary>
    public ErrorReportStatus Status { get; private set; } = ErrorReportStatus.New;

    /// <summary>Son durum değişikliği (UTC).</summary>
    public DateTimeOffset? StatusChangedAtUtc { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni bildirim (durum <see cref="ErrorReportStatus.New"/>); boş isteğe bağlı alanlar <c>null</c> olur, uzunlar kırpılır.</summary>
    /// <param name="userId">Bildiren.</param>
    /// <param name="source">Geldiği yer.</param>
    /// <param name="message">Açıklama (doğrulanmış, boş değil).</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="pageUrl">Sayfa.</param>
    /// <param name="userAgent">Tarayıcı.</param>
    /// <param name="appVersion">Sürüm.</param>
    /// <param name="machine">Makine.</param>
    /// <returns>Bildirim.</returns>
    public static ErrorReport Create(
        Guid userId,
        ErrorReportSource source,
        string message,
        string? details,
        string? pageUrl,
        string? userAgent,
        string? appVersion,
        string? machine) => new()
        {
            UserId = userId,
            Source = source,
            Message = Clip(message?.Trim(), MaxMessageLength) ?? string.Empty,
            Details = Clip(details, MaxDetailsLength),
            PageUrl = Clip(pageUrl?.Trim(), MaxPageUrlLength),
            UserAgent = Clip(userAgent?.Trim(), MaxUserAgentLength),
            AppVersion = Clip(appVersion?.Trim(), MaxAppVersionLength),
            Machine = Clip(machine?.Trim(), MaxMachineLength),
        };

    /// <summary>Geçiş izinli mi (aynı duruma geçiş izinlidir).</summary>
    /// <param name="from">Mevcut durum.</param>
    /// <param name="to">Hedef durum.</param>
    /// <returns>İzinliyse <c>true</c>.</returns>
    public static bool CanTransition(ErrorReportStatus from, ErrorReportStatus to) => (from, to) switch
    {
        _ when from == to => true,
        (ErrorReportStatus.New, ErrorReportStatus.Seen) => true,
        (_, ErrorReportStatus.Resolved) => true,
        (ErrorReportStatus.Resolved, ErrorReportStatus.New) => true,
        _ => false,
    };

    /// <summary>Durumu değiştirir; izinsiz geçişte doğrulama hatası döner, aynı durumda hiçbir şey yapmaz.</summary>
    /// <param name="target">Hedef durum.</param>
    /// <param name="nowUtc">Şu an (UTC).</param>
    /// <returns>Sonuç.</returns>
    public Result ChangeStatus(ErrorReportStatus target, DateTimeOffset nowUtc)
    {
        if (!Enum.IsDefined(target) || !CanTransition(Status, target))
        {
            return DiagnosticsErrors.InvalidStatusTransition(Status, target);
        }

        if (Status != target)
        {
            Status = target;
            StatusChangedAtUtc = nowUtc;
        }

        return Result.Success();
    }

    private static string? Clip(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= max ? value : value[..(max - 1)] + "…";
    }
}
