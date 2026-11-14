namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Seslendirme sonucu (CallLog'a yazılmış maliyetle).
/// </summary>
/// <param name="Audio">Ses içeriği.</param>
/// <param name="MimeType">MIME türü.</param>
/// <param name="AudioSeconds">Ses süresi (saniye).</param>
/// <param name="ModelKey">Katalog anahtarı.</param>
/// <param name="CostUsd">Maliyet ($).</param>
/// <param name="DurationMs">Süre (ms).</param>
public sealed record SpeechResult(byte[] Audio, string MimeType, decimal? AudioSeconds, string ModelKey, decimal CostUsd, long DurationMs);
