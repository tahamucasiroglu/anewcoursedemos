namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Bileşenin donanıma göre seçilen biçimi (spec 0028 Req 5): ör. PyTorch ortamının CUDA ve CPU paketleri. Liste sırasıyla ilk uyan seçilir;
/// <see cref="Gpu"/> <c>null</c> ise her bilgisayara uyar (son seçenek). Kilit dosyası ve boyutlar varyanta göredir.
/// </summary>
/// <param name="Id">Varyant adı (ör. <c>cuda</c>, <c>cpu</c>); sürüme <c>+id</c> olarak eklenir.</param>
/// <param name="Gpu">Gereken ekran kartı üreticisi (<c>nvidia</c>) ya da <c>null</c>.</param>
/// <param name="MinVramMb">Gereken en az ayrılmış bellek (MB); <see cref="Gpu"/> <c>null</c> ise yok sayılır.</param>
/// <param name="Sha256">Bu varyantın kilit dosyasının/arşivinin SHA-256'sı.</param>
/// <param name="DownloadBytes">İndirme boyutu.</param>
/// <param name="InstalledBytes">Kurulu boyut.</param>
public sealed record ComponentVariant(string Id, string? Gpu, int MinVramMb, string Sha256, long DownloadBytes, long InstalledBytes);
