namespace Konsept.Shared.Kernel.Abstract;

/// <summary>
/// Tek ürün versiyonunu (Web, Api ve Ajan ortak) sağlar. Değer sabit metin değil, derlemenin versiyon
/// bilgisinden (<c>Directory.Build.props</c>) okunur.
/// </summary>
public interface IProductVersionProvider
{
    /// <summary>Gets ürün versiyonu, ör. <c>0.1.0</c>.</summary>
    string Version { get; }
}
