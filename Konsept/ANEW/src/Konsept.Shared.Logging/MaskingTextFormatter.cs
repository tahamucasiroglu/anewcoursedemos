using System.Globalization;
using Konsept.Shared.Kernel.Security;
using Serilog.Events;
using Serilog.Formatting;

namespace Konsept.Shared.Logging;

/// <summary>
/// Dosya ve konsol çıktısını yazmadan hemen önce son kez maskeler: mesaj şablonunun kendisine ya da exception
/// metnine gömülmüş gizli bilgiler de yakalanır (spec 0008 Req 8–9). Çerçeve adaptörü; mantık <see cref="SecretMasker"/>'dadır.
/// </summary>
/// <param name="inner">Asıl biçimlendirici.</param>
public sealed class MaskingTextFormatter(ITextFormatter inner) : ITextFormatter
{
    /// <inheritdoc />
    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        using var buffer = new StringWriter(CultureInfo.InvariantCulture);
        inner.Format(logEvent, buffer);
        output.Write(SecretMasker.Mask(buffer.ToString()));
    }
}
