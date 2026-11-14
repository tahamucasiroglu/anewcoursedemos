namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// <c>IProviderGateway</c> metin çağrısının sonucu: tipli değer + ham metin + kullanım + maliyet (CallLog'a zaten yazılmıştır).
/// JSON onarım denemesi yapıldıysa <see cref="Repaired"/> true; kullanım ve maliyet iki çağrının toplamıdır.
/// </summary>
/// <typeparam name="T">Değer türü (<c>string</c> ya da JSON'dan çözülen tür).</typeparam>
public sealed record TextResult<T>
{
    /// <summary>Değer.</summary>
    public required T Value { get; init; }

    /// <summary>Modelin döndürdüğü ham metin (son çağrı).</summary>
    public string RawText { get; init; } = string.Empty;

    /// <summary>Katalog anahtarı.</summary>
    public string ModelKey { get; init; } = string.Empty;

    /// <summary>Sağlayıcı model kimliği.</summary>
    public string ProviderModelId { get; init; } = string.Empty;

    /// <summary>Toplam kullanım.</summary>
    public CallUsage Usage { get; init; } = CallUsage.Empty;

    /// <summary>Toplam maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Toplam süre (ms).</summary>
    public long DurationMs { get; init; }

    /// <summary>Durma nedeni.</summary>
    public string? StopReason { get; init; }

    /// <summary>JSON onarımı yapıldı mı.</summary>
    public bool Repaired { get; init; }
}
