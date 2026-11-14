using Microsoft.Extensions.Logging;

namespace TahaKonsept.Modules.Providers.Application.TextModels.Base;

/// <summary>
/// Metin modeli log mesajları (kaynak üretimli, yüksek performanslı <c>LoggerMessage</c>).
/// </summary>
internal static partial class TextModelLog
{
    /// <summary>JSON onarımı başlıyor.</summary>
    /// <param name="logger">Logger.</param>
    /// <param name="model">Model.</param>
    /// <param name="error">Çözme hatası.</param>
    [LoggerMessage(EventId = 7101, Level = LogLevel.Warning, Message = "{Model} geçersiz JSON döndürdü, onarım deneniyor: {Error}")]
    public static partial void LogJsonRepair(this ILogger logger, string model, string error);

    /// <summary>Metin çıktısı kesildi.</summary>
    /// <param name="logger">Logger.</param>
    /// <param name="model">Model.</param>
    /// <param name="maxTokens">Sınır.</param>
    [LoggerMessage(EventId = 7102, Level = LogLevel.Warning, Message = "{Model} çıktısı {MaxTokens} token sınırında kesildi.")]
    public static partial void LogTruncated(this ILogger logger, string model, int maxTokens);
}
