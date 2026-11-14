using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Abstractions.Speech;

namespace TahaKonsept.Agent.Abstractions.Speech.Abstract;

/// <summary>
/// Yerel seslendirme motoru (Supertonic, Chatterbox, XTTS, Piper…). Metni (okunuş) ses dosyasına çevirir.
/// <see cref="SupportsLanguage"/> kurulumdan önce dil kontrolü için; <see cref="ReleaseAsync"/> ses aşaması bitince GPU belleğini boşaltır.
/// </summary>
public interface ISpeechEngine : IAgentEngine
{
    /// <summary>Dil etiketi (ör. <c>Sadece Türkçe</c>, <c>Çok dilli</c>).</summary>
    string LanguageLabel { get; }

    /// <summary>Desteklenen dil kodları (boşsa hepsi).</summary>
    IReadOnlyList<string> Languages { get; }

    /// <summary>Referans sesle klonlama destekleniyor mu.</summary>
    bool SupportsCloning { get; }

    /// <summary>Seçilebilir hazır sesler (id → ad).</summary>
    IReadOnlyDictionary<string, string> Voices { get; }

    /// <summary>Metni seslendirir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ses dosyası ve süresi.</returns>
    Task<Result<SpeechOutput>> SynthesizeAsync(SpeechRequest request, CancellationToken ct);

    /// <summary>Dil kodu destekleniyor mu (<see cref="Languages"/> boşsa hepsi; <c>tr-TR</c> → <c>tr</c>).</summary>
    /// <param name="language">Dil kodu.</param>
    /// <returns>Destekleniyorsa <c>true</c>.</returns>
    bool SupportsLanguage(string? language);

    /// <summary>
    /// Yüklü modeli bırakır (GPU motorlarında Python işçisi kapanır, VRAM boşalır; sonraki istekte yeniden yüklenir).
    /// Süreç içi / CPU motorlarında bir şey yapmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    Task ReleaseAsync();
}
