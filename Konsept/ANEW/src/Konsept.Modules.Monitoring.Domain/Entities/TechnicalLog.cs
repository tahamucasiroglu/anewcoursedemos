using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Shared.Kernel.Security;

namespace Konsept.Modules.Monitoring.Domain.Entities;

/// <summary>
/// Veritabanındaki teknik log kaydı (spec 0008 Req 6–7): Api ve Web'in Warning+ logları ile ajanın Error/Critical
/// özetleri, 30 gün. Mesaj ve exception metni oluşturulurken maskelenir (Req 8 — kalıcı kayıt sınırında son güvence).
/// </summary>
public sealed class TechnicalLog
{
    /// <summary>Mesajın en fazla uzunluğu; daha uzunu kırpılır.</summary>
    public const int MessageMaxLength = 4000;

    /// <summary>Exception metninin en fazla uzunluğu; daha uzunu kırpılır.</summary>
    public const int ExceptionMaxLength = 8000;

    /// <summary>Kısa metin alanlarının (korelasyon, kategori, sürüm) en fazla uzunluğu.</summary>
    public const int ShortMaxLength = 200;

    private TechnicalLog(Guid id, DateTimeOffset occurredAt, TechnicalLogLevel level, LogSource source, string message)
    {
        Id = id;
        OccurredAt = occurredAt;
        Level = level;
        Source = source;
        Message = message;
    }

    /// <summary>Gets kayıt kimliği (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets log zamanı (UTC).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Gets seviye.</summary>
    public TechnicalLogLevel Level { get; private set; }

    /// <summary>Gets kaynak uygulama.</summary>
    public LogSource Source { get; private set; }

    /// <summary>Gets maskelenmiş mesaj.</summary>
    public string Message { get; private set; }

    /// <summary>Gets maskelenmiş exception metni.</summary>
    public string? Exception { get; private set; }

    /// <summary>Gets korelasyon numarası.</summary>
    public string? CorrelationId { get; private set; }

    /// <summary>Gets log kategorisi (kaynak sınıf).</summary>
    public string? Category { get; private set; }

    /// <summary>Gets ajan sürümü (yalnızca ajan özetinde).</summary>
    public string? AgentVersion { get; private set; }

    /// <summary>Yeni bir teknik log kaydı oluşturur; metinler maskelenir ve sınırlara kırpılır.</summary>
    /// <param name="occurredAt">Log zamanı; UTC'ye çevrilir.</param>
    /// <param name="level">Seviye.</param>
    /// <param name="source">Kaynak uygulama.</param>
    /// <param name="message">Mesaj; boşsa "(boş)".</param>
    /// <param name="exception">Exception metni.</param>
    /// <param name="correlationId">Korelasyon numarası.</param>
    /// <param name="category">Kategori.</param>
    /// <param name="agentVersion">Ajan sürümü.</param>
    /// <returns>Yeni kayıt.</returns>
    public static TechnicalLog Create(
        DateTimeOffset occurredAt,
        TechnicalLogLevel level,
        LogSource source,
        string? message,
        string? exception = null,
        string? correlationId = null,
        string? category = null,
        string? agentVersion = null)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "(boş)" : message;
        return new TechnicalLog(Guid.CreateVersion7(occurredAt), occurredAt.ToUniversalTime(), level, source, Clip(SecretMasker.Mask(text), MessageMaxLength)!)
        {
            Exception = exception is null ? null : Clip(SecretMasker.Mask(exception), ExceptionMaxLength),
            CorrelationId = Clip(correlationId, ShortMaxLength),
            Category = Clip(category, ShortMaxLength),
            AgentVersion = Clip(agentVersion, ShortMaxLength),
        };
    }

    private static string? Clip(string? value, int max) =>
        value is null ? null : value.Length > max ? value[..max] : value;
}
