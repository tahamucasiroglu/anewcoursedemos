namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Görsel üretimi sonucu (CallLog'a yazılmış maliyetle).
/// </summary>
/// <param name="Images">Görseller.</param>
/// <param name="MimeType">MIME türü.</param>
/// <param name="ModelKey">Katalog anahtarı.</param>
/// <param name="CostUsd">Maliyet ($).</param>
/// <param name="DurationMs">Süre (ms).</param>
public sealed record ImageResult(IReadOnlyList<byte[]> Images, string MimeType, string ModelKey, decimal CostUsd, long DurationMs);
