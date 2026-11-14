using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Abstract;

/// <summary>
/// Kullanıcıya görünen işlem günlüğüne (alt Günlük şeridi + yönetim paneli) kayıt yazar. Diğer modüller bunu enjekte eder.
/// Singleton'dır (arka plan servislerinden de kullanılabilir); her yazım kendi DB scope'unda yapılır ve <b>asla istisna fırlatmaz</b>
/// (yazılamazsa Serilog'a uyarı düşer). Kullanıcı verilmezse istekteki oturum kullanıcısı, o da yoksa <see cref="Guid.Empty"/> (sistem) yazılır.
/// Örnek: <c>await logger.LogTimedAsync(ActivitySources.Production, "Video hazırlandı", start, end, ct: ct);</c>
/// → "Video hazırlandı — süre 57 sn, başlangıç 10:30, bitiş 11:00" (Türkiye saati).
/// </summary>
public interface IActivityLogger
{
    /// <summary>Kayıt yazar.</summary>
    /// <param name="level">Seviye.</param>
    /// <param name="source">Kaynak (bkz. <see cref="ActivitySources"/>).</param>
    /// <param name="message">Türkçe mesaj.</param>
    /// <param name="details">Ek ayrıntı: herhangi bir nesne (JSON'a çevrilir), JSON metni ya da düz metin; isteğe bağlı.</param>
    /// <param name="durationMs">Süre (ms); isteğe bağlı.</param>
    /// <param name="userId">Sahip kullanıcı; verilmezse oturum kullanıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task LogAsync(
        LogLevelKind level,
        string source,
        string message,
        object? details = null,
        long? durationMs = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Bilgi kaydı.</summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="durationMs">Süre (ms).</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task InformationAsync(string source, string message, object? details = null, long? durationMs = null, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>Uyarı kaydı.</summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task WarningAsync(string source, string message, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>Hata kaydı.</summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı (ör. istisna metni).</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task ErrorAsync(string source, string message, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>Kritik kayıt.</summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task CriticalAsync(string source, string message, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Süreli işlem kaydı: mesajın sonuna "— süre 57 sn, başlangıç 10:30, bitiş 11:00" eklenir (Türkiye saati), süre ms olarak saklanır.
    /// </summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj (ör. "Video hazırlandı").</param>
    /// <param name="startedAtUtc">Başlangıç.</param>
    /// <param name="finishedAtUtc">Bitiş.</param>
    /// <param name="level">Seviye (varsayılan bilgi).</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task LogTimedAsync(
        string source,
        string message,
        DateTimeOffset startedAtUtc,
        DateTimeOffset finishedAtUtc,
        LogLevelKind level = LogLevelKind.Information,
        object? details = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
