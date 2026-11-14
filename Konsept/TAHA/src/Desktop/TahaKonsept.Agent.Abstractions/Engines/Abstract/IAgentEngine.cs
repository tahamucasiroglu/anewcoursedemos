using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Abstractions.Engines.Abstract;

/// <summary>
/// Ajanda çalışan tüm yerel motorların (video, ses, görsel) ortak sözleşmesi.
/// <see cref="Key"/> katalogdaki model/motor anahtarıyla birebir eşleşir; <see cref="EnsureReadyAsync"/> eksik bileşeni indirir/kurar.
/// </summary>
public interface IAgentEngine
{
    /// <summary>Katalog anahtarı (ör. <c>hyperframes</c>, <c>supertonic-3</c>, <c>z-image-turbo</c>).</summary>
    string Key { get; }

    /// <summary>Kullanıcıya gösterilen ad.</summary>
    string DisplayName { get; }

    /// <summary>Gerekli en az VRAM (MB); GPU gerekmiyorsa <c>null</c>.</summary>
    int? MinVramMb { get; }

    /// <summary>Motorun çalışması için gereken her şey kurulu mu (hızlı, ağ kullanmadan).</summary>
    /// <returns>Kuruluysa <c>true</c>.</returns>
    bool IsInstalled();

    /// <summary>Eksik bileşenleri (model dosyası, venv, npm paketi…) kurar; zaten hazırsa hemen döner.</summary>
    /// <param name="progress">Kurulum ilerlemesi (isteğe bağlı).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    Task<Result> EnsureReadyAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct);
}
