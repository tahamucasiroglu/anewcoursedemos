using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Monitoring.Domain.Rules;

namespace Konsept.Modules.Monitoring.Domain.Entities;

/// <summary>
/// Bir hata bildirimi (spec 0022 Req 1, 4, 10, 11): kaynak (web / ajan / API), "Yeni" durumu, zaman (UTC), ürün versiyonu ve kaynağa göre
/// ekler. Süresiz saklanır. Her metin alanı oluşturulurken maskelenir (H-2); ajan bildiriminde makine adı alanı yoktur.
/// Kullanıcıya gösterilen sıra numarası (<see cref="Number"/>) veritabanında üretilir.
/// </summary>
public sealed class ErrorReport
{
    private ErrorReport(Guid id, ErrorReportSource source, DateTimeOffset createdAt, Guid? userId, string productVersion)
    {
        Id = id;
        Source = source;
        Status = ErrorReportStatus.New;
        CreatedAt = createdAt;
        UserId = userId;
        ProductVersion = productVersion;
    }

    /// <summary>Gets kimlik (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets kullanıcıya gösterilen bildirim numarası (sıralı).</summary>
    public long Number { get; private set; }

    /// <summary>Gets kaynak.</summary>
    public ErrorReportSource Source { get; private set; }

    /// <summary>Gets durum (her zaman "Yeni" başlar).</summary>
    public ErrorReportStatus Status { get; private set; }

    /// <summary>Gets oluşma zamanı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets gönderen kullanıcı; oturumsuz API kaydında <c>null</c>.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Gets ürün (API) versiyonu.</summary>
    public string ProductVersion { get; private set; }

    /// <summary>Gets kullanıcının açıklaması (web) ya da hata özeti (ajan, API); maskeli.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets bulunulan sayfa (web).</summary>
    public string? Page { get; private set; }

    /// <summary>Gets korelasyon numarası (web: son hatanınki; API: isteğinki).</summary>
    public string? CorrelationId { get; private set; }

    /// <summary>Gets ajan versiyonu (ajan).</summary>
    public string? AgentVersion { get; private set; }

    /// <summary>Gets işletim sistemi (ajan).</summary>
    public string? OperatingSystem { get; private set; }

    /// <summary>Gets hatalı bileşen (ajan kurulum/güncelleme).</summary>
    public string? Component { get; private set; }

    /// <summary>Gets hatanın adımı: indirme / doğrulama / kurulum (ajan kurulum/güncelleme).</summary>
    public string? Step { get; private set; }

    /// <summary>Gets ekli satırlar: web'de son 20 Günlük kaydı, ajanda log satırları; maskeli, satır satır.</summary>
    public string? Attachments { get; private set; }

    /// <summary>Web'den gelen bildirimi oluşturur.</summary>
    /// <param name="userId">Gönderen.</param>
    /// <param name="createdAt">Zaman.</param>
    /// <param name="productVersion">Ürün versiyonu.</param>
    /// <param name="description">Açıklama.</param>
    /// <param name="page">Bulunulan sayfa.</param>
    /// <param name="correlationId">Son hatanın korelasyon numarası.</param>
    /// <param name="journalLines">Son Günlük kayıtları.</param>
    /// <returns>Bildirim.</returns>
    public static ErrorReport CreateWeb(Guid userId, DateTimeOffset createdAt, string productVersion, string description, string? page, string? correlationId, IEnumerable<string> journalLines)
    {
        ArgumentNullException.ThrowIfNull(journalLines);
        var report = New(ErrorReportSource.Web, createdAt, userId, productVersion);
        report.Description = ErrorReportRules.Clean(description, ErrorReportRules.DescriptionMaxLength);
        report.Page = ErrorReportRules.Clean(page, ErrorReportRules.ShortFieldMaxLength);
        report.CorrelationId = ErrorReportRules.Clean(correlationId, ErrorReportRules.ShortFieldMaxLength);
        report.Attachments = Join(journalLines);
        return report;
    }

    /// <summary>Ajandan gelen bildirimi oluşturur (makine adı alanı yoktur).</summary>
    /// <param name="userId">Ajanın sahibi.</param>
    /// <param name="createdAt">Zaman.</param>
    /// <param name="productVersion">Ürün versiyonu.</param>
    /// <param name="summary">Hata özeti.</param>
    /// <param name="agentVersion">Ajan versiyonu.</param>
    /// <param name="operatingSystem">İşletim sistemi.</param>
    /// <param name="component">Bileşen.</param>
    /// <param name="step">Adım.</param>
    /// <param name="logLines">Log satırları (en yeni 200 alınır).</param>
    /// <returns>Bildirim.</returns>
    public static ErrorReport CreateAgent(
        Guid userId,
        DateTimeOffset createdAt,
        string productVersion,
        string summary,
        string? agentVersion,
        string? operatingSystem,
        string? component,
        string? step,
        IEnumerable<string> logLines)
    {
        ArgumentNullException.ThrowIfNull(logLines);
        var report = New(ErrorReportSource.Agent, createdAt, userId, productVersion);
        report.Description = ErrorReportRules.Clean(summary, ErrorReportRules.SummaryMaxLength);
        report.AgentVersion = ErrorReportRules.Clean(agentVersion, ErrorReportRules.ShortFieldMaxLength);
        report.OperatingSystem = ErrorReportRules.Clean(operatingSystem, ErrorReportRules.ShortFieldMaxLength);
        report.Component = ErrorReportRules.Clean(component, ErrorReportRules.ShortFieldMaxLength);
        report.Step = ErrorReportRules.Clean(step, ErrorReportRules.ShortFieldMaxLength);
        report.Attachments = Join(logLines.TakeLast(ErrorReportRules.MaxLogLines));
        return report;
    }

    /// <summary>API'nin beklenmeyen hatasından otomatik kaydı oluşturur.</summary>
    /// <param name="userId">İstek oturumluysa kullanıcı.</param>
    /// <param name="createdAt">Zaman.</param>
    /// <param name="productVersion">Ürün versiyonu.</param>
    /// <param name="summary">Hata özeti (maskelenir).</param>
    /// <param name="correlationId">İsteğin korelasyon numarası.</param>
    /// <returns>Bildirim.</returns>
    public static ErrorReport CreateApi(Guid? userId, DateTimeOffset createdAt, string productVersion, string summary, string? correlationId)
    {
        var report = New(ErrorReportSource.Api, createdAt, userId, productVersion);
        report.Description = ErrorReportRules.Clean(summary, ErrorReportRules.SummaryMaxLength);
        report.CorrelationId = ErrorReportRules.Clean(correlationId, ErrorReportRules.ShortFieldMaxLength);
        return report;
    }

    /// <summary>Numarayı atar (bellek depoları için; veritabanında sütun kendisi üretir).</summary>
    /// <param name="number">Numara.</param>
    public void AssignNumber(long number) => Number = number;

    private static ErrorReport New(ErrorReportSource source, DateTimeOffset createdAt, Guid? userId, string productVersion) =>
        new(Guid.CreateVersion7(createdAt), source, createdAt.ToUniversalTime(), userId, ErrorReportRules.Clean(productVersion, ErrorReportRules.ShortFieldMaxLength) ?? "?");

    private static string? Join(IEnumerable<string> lines) =>
        ErrorReportRules.Clean(string.Join('\n', lines.Where(l => !string.IsNullOrWhiteSpace(l))), ErrorReportRules.AttachmentMaxLength);
}
