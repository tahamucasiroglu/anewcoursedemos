namespace Konsept.Web.ApiClient.Dtos;

/// <summary>
/// Günlük şeridinde ve geçmişinde gösterilen olay (spec 0021).
/// </summary>
/// <param name="Type">Olay türü.</param>
/// <param name="Level">Seviye: 0 bilgi, 1 uyarı, 2 hata, 3 kritik.</param>
/// <param name="Message">Türkçe metin.</param>
/// <param name="OccurredAt">Olay zamanı (UTC).</param>
/// <param name="Id">Olay kimliği (yeni kayıt duyurusu ve geçmişte tekilleştirme için).</param>
/// <param name="StartedAt">Süreli işlemin başlangıcı (UTC).</param>
/// <param name="EndedAt">Süreli işlemin bitişi (UTC).</param>
/// <param name="ProfileName">Profil etiketi.</param>
public sealed record SystemEventResponse(
    string Type,
    int Level,
    string Message,
    DateTimeOffset OccurredAt,
    Guid? Id = null,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? EndedAt = null,
    string? ProfileName = null);
