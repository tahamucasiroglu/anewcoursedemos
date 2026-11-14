using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Abstractions.Speech;

namespace TahaKonsept.Agent.Abstractions.Speech.Abstract;

/// <summary>Yerel seslendirme motoru (Supertonic, Chatterbox, XTTS, Piper…). Metni (okunuş) ses dosyasına çevirir.</summary>
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
}
