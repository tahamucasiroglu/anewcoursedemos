namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Bir online ses modelinin seçilebilir sesi (<c>GET api/providers/voices?modelKey=</c> yanıt öğesi).
/// <see cref="Id"/> sağlayıcıya giden ses kimliğidir (ElevenLabs voice_id, Azure ses adı, Gemini prebuilt ses adı…).
/// Liste sırası: önce Türkçe'ye özgü sesler, sonra varsayılan, sonra diğerleri.
/// </summary>
public sealed record VoiceInfo
{
    /// <summary>Sağlayıcı ses kimliği (ör. <c>tr-TR-AhmetNeural</c>, <c>Kore</c>, <c>21m00Tcm4TlvDq8ikWAM</c>).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Görünen ad.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Dil (BCP-47, ör. <c>tr-TR</c>; çok dilli seslerde <c>multi</c>).</summary>
    public string Language { get; init; } = "multi";

    /// <summary>Cinsiyet etiketi (<c>Kadın</c>, <c>Erkek</c>, <c>Nötr</c>).</summary>
    public string Gender { get; init; } = string.Empty;

    /// <summary>Ses Türkçe için özel hazırlanmış mı (ör. Azure tr-TR sesleri).</summary>
    public bool NativeTurkish { get; init; }

    /// <summary>Model için varsayılan ses mi (istek ses vermezse bu kullanılır).</summary>
    public bool IsDefault { get; init; }

    /// <summary>Not (ton, doğrulanmamış kimlik vb.).</summary>
    public string? Notes { get; init; }
}
