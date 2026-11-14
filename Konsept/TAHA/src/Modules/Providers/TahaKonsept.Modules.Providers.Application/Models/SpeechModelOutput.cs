namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Bir ses sağlayıcısının ham çıktısı: ses baytları, MIME türü, süre ve kullanım.
/// </summary>
/// <param name="Audio">Ses dosyası içeriği.</param>
/// <param name="MimeType">MIME türü.</param>
/// <param name="AudioSeconds">Ses süresi (saniye; bilinmiyorsa null).</param>
/// <param name="Usage">Kullanım (karakter / saniye / token).</param>
public sealed record SpeechModelOutput(byte[] Audio, string MimeType, decimal? AudioSeconds, CallUsage Usage);
