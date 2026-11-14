using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Her ücretlendirilebilir çağrıyı fiyatlayıp CallLog'a yazar ve maliyeti döner. Hata fırlatmaz (kayıt başarısızsa loglar).
/// Paralel çağrılara güvenlidir (her kayıt kendi kapsamında).
/// </summary>
public interface ICallRecorder
{
    /// <summary>Çağrıyı kaydeder.</summary>
    /// <param name="request">Ölçüm.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Hesaplanan maliyet ($).</returns>
    Task<decimal> RecordAsync(CallRecordRequest request, CancellationToken ct);
}
