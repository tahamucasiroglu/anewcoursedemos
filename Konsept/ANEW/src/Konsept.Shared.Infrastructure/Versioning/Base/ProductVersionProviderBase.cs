using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Shared.Infrastructure.Versioning.Base;

/// <summary>
/// Versiyon sağlayıcılarının ortak temeli: ham versiyon metnini kaynak bilgisinden (<c>+sha</c>)
/// arındırır.
/// </summary>
public abstract class ProductVersionProviderBase : IProductVersionProvider
{
    /// <inheritdoc />
    public string Version => Normalize(ReadRawVersion());

    /// <summary>Ham versiyon metnini okur.</summary>
    /// <returns>Ham versiyon (ör. <c>0.1.0+abc123</c>).</returns>
    protected abstract string? ReadRawVersion();

    private static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "0.0.0";
        }

        var plus = raw.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? raw : raw[..plus];
    }
}
