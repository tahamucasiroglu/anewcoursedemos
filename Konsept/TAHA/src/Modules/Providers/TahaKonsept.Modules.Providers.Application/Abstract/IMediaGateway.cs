using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Diğer modüllerin (Production) online GÖRSEL ve SES modellerine eriştiği GENEL arayüz. Model kataloğundan modeli bulur,
/// kullanıcının şifreli anahtarını çözer, uygun somut <c>IImageModel</c> / <c>ISpeechModel</c>'i çalıştırır; her çağrı CallLog'a
/// maliyetiyle yazılır. Kullanıcı kimliği ve amaç <see cref="ImageRequest.Context"/> / <see cref="SpeechRequest.Context"/> içindedir.
/// Bütçe kontrolü burada YAPILMAZ (bkz. <see cref="IBudgetGuard"/>).
/// Önemli üyeler: <see cref="GenerateImageAsync"/>, <see cref="SynthesizeSpeechAsync"/>.
/// </summary>
public interface IMediaGateway
{
    /// <summary>Katalogdaki online görsel modeliyle (<paramref name="modelKey"/>) görsel üretir.</summary>
    /// <param name="modelKey">Katalog anahtarı (ör. <c>gemini-nano-banana-2.1</c>).</param>
    /// <param name="request">Görsel isteği.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görsel baytları ve maliyet ya da hata (<c>providers.*</c> kodları).</returns>
    Task<Result<ImageResult>> GenerateImageAsync(string modelKey, ImageRequest request, CancellationToken ct);

    /// <summary>Katalogdaki online ses modeliyle (<paramref name="modelKey"/>) metni seslendirir.</summary>
    /// <param name="modelKey">Katalog anahtarı (ör. <c>gemini-3.8-flash-tts</c>).</param>
    /// <param name="request">Ses isteği (metin zaten Normalize edilmiş okunuş metnidir).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ses baytları, süre ve maliyet ya da hata.</returns>
    Task<Result<SpeechResult>> SynthesizeSpeechAsync(string modelKey, SpeechRequest request, CancellationToken ct);
}
