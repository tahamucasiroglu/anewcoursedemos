using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Abstractions.Tools.Abstract;

/// <summary>
/// Eksik harici aracı (uv, Python, Node, FFmpeg…) gerektiğinde kurar. <see cref="IToolLocator"/> yalnız bulur; bu arayüz bulamazsa
/// ilgili ön gereksinim bileşenini indirip kurar. Motorlar (ör. Python ses motorları) kurulum sırasında kullanır; böylece kullanıcı
/// web'den bir motor seçtiğinde masaüstü kurulum ekranına gitmesi gerekmez.
/// Önemli üye: <see cref="EnsureAsync"/>.
/// </summary>
public interface IToolProvisioner
{
    /// <summary>Aracı bulur; yoksa kurar ve yolunu döner.</summary>
    /// <param name="kind">Araç.</param>
    /// <param name="progress">Kurulum ilerlemesi (isteğe bağlı).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Aracın yolu ya da hata (kurulamadıysa ya da bu araç otomatik kurulamıyorsa).</returns>
    Task<Result<string>> EnsureAsync(ToolKind kind, IProgress<EngineSetupProgress>? progress, CancellationToken ct);
}
