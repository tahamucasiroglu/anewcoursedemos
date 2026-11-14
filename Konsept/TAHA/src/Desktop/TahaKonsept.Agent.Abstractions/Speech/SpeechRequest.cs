namespace TahaKonsept.Agent.Abstractions.Speech;

/// <summary>Seslendirme isteği.</summary>
/// <param name="Text">Okunuş metni (Normalize aşamasından geçmiş).</param>
/// <param name="Language">Dil kodu (ör. <c>tr</c>).</param>
/// <param name="VoiceId">Hazır ses kimliği (isteğe bağlı).</param>
/// <param name="ReferenceAudioPath">Klonlama için referans ses (isteğe bağlı).</param>
/// <param name="Speed">Hız çarpanı (1.0 normal).</param>
/// <param name="OutputPath">Çıktı .wav yolu.</param>
public sealed record SpeechRequest(string Text, string Language, string? VoiceId, string? ReferenceAudioPath, double Speed, string OutputPath);
