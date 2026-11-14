namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Bir metin sağlayıcısının ham çıktısı (tek HTTP çağrısı): metin, kullanım, durma nedeni, kesilme bilgisi.
/// </summary>
/// <param name="Text">Yanıt metni (birden çok metin bloğu birleştirilmiş).</param>
/// <param name="Usage">Kullanım.</param>
/// <param name="StopReason">Durma nedeni (sağlayıcının değeri).</param>
/// <param name="Truncated">Çıkış token sınırına takıldı mı.</param>
/// <param name="ProviderModelId">Sağlayıcının bildirdiği model kimliği.</param>
public sealed record ModelOutput(string Text, CallUsage Usage, string? StopReason, bool Truncated, string? ProviderModelId);
