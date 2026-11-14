using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Abstract;

/// <summary>
/// Ajanın ihtiyaç duyduğu harici bileşen (Node, FFmpeg, uv, Python, Chrome Headless Shell, VC++ Redist…).
/// <see cref="IsInstalled"/> hızlıdır (ağ yok); <see cref="InstallAsync"/> indirir/kurar. İsteğe bağlı bileşenler
/// (<see cref="IsOptional"/>, ör. Python + CUDA, Chatterbox için) kullanıcı seçerse kurulur.
/// </summary>
public interface IPrerequisite
{
    /// <summary>Kimlik (ör. <c>node</c>, <c>ffmpeg</c>); <c>InstalledComponent.Id</c> ile aynı.</summary>
    string Id { get; }

    /// <summary>Kullanıcıya gösterilen ad.</summary>
    string DisplayName { get; }

    /// <summary>Kısa açıklama (neden gerekli).</summary>
    string Description { get; }

    /// <summary>İsteğe bağlı mı.</summary>
    bool IsOptional { get; }

    /// <summary>Kurulum ekranındaki grup adı (ör. <c>Temel</c>, <c>Python + CUDA (Chatterbox vb.)</c>).</summary>
    string Group { get; }

    /// <summary>Kurulum sırası (küçük önce; bağımlılıklar için).</summary>
    int Order { get; }

    /// <summary>Kurulu sürüm (biliniyorsa).</summary>
    string? InstalledVersion { get; }

    /// <summary>Kurulu ve kullanılabilir mi.</summary>
    /// <returns>Kuruluysa <c>true</c>.</returns>
    bool IsInstalled();

    /// <summary>Bileşeni kurar (zaten kuruluysa hemen başarı döner).</summary>
    /// <param name="progress">Adım ilerlemesi (mesaj + yüzde).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> InstallAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct);
}
