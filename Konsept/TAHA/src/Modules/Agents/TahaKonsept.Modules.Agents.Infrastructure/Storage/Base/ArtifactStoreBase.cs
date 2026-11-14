using System.Text;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Infrastructure.Storage.Base;

/// <summary>
/// Artefakt deposu tabanı: kök klasör (<see cref="RootFolderName"/> = <c>App_Data/artifacts</c>), göreli yol çözümü
/// (<see cref="ResolvePath"/>; kök dışına çıkışı engeller), ad/uzantı doğrulama (<see cref="IsValidName"/>, <see cref="SafeExtension"/>)
/// ve indirme adresi (<see cref="DownloadUrl"/>). Okuma/yazma türetilen sınıftadır.
/// </summary>
public abstract class ArtifactStoreBase : IArtifactStore
{
    /// <summary>App_Data altındaki kök klasör adı.</summary>
    public const string RootFolderName = "artifacts";

    /// <summary>Oluşturur.</summary>
    /// <param name="appData">Uygulama veri klasörü.</param>
    protected ArtifactStoreBase(IAppDataLocation appData)
    {
        AppData = appData ?? throw new ArgumentNullException(nameof(appData));
    }

    /// <summary>Uygulama veri klasörü.</summary>
    protected IAppDataLocation AppData { get; }

    /// <summary>Kaydeder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="name">Ad.</param>
    /// <param name="contentType">İçerik türü.</param>
    /// <param name="content">İçerik.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İndirme bilgisi.</returns>
    public abstract Task<Result<ArtifactDownload>> SaveAsync(
        Guid userId,
        Guid jobId,
        string name,
        string? contentType,
        Stream content,
        int? sceneIndex = null,
        CancellationToken cancellationToken = default);

    /// <summary>Açar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="artifactId">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İçerik.</returns>
    public abstract Task<Result<ArtifactContent>> OpenAsync(Guid userId, Guid artifactId, CancellationToken cancellationToken = default);

    /// <summary>Siler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="artifactId">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public abstract Task<Result> DeleteAsync(Guid userId, Guid artifactId, CancellationToken cancellationToken = default);

    /// <summary>İşin artefaktlarını siler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen sayısı.</returns>
    public abstract Task<Result<int>> DeleteJobArtifactsAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>Artefaktın göreli indirme adresi.</summary>
    /// <param name="artifactId">Artefakt.</param>
    /// <returns><c>api/agents/artifacts/{id}</c>.</returns>
    protected static string DownloadUrl(Guid artifactId) => "api/agents/artifacts/" + artifactId.ToString("D");

    /// <summary>Mantıksal ad geçerli mi (boş değil, ≤ 200, yol ayracı/geçersiz karakter yok).</summary>
    /// <param name="name">Ad.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    protected static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Length <= 200
        && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
        && name.IndexOfAny(['/', '\\']) < 0
        && name is not "." and not "..";

    /// <summary>Addan güvenli uzantı (yalnızca harf/rakam, ≤ 10; yoksa boş).</summary>
    /// <param name="name">Ad.</param>
    /// <returns>Uzantı (noktalı) ya da boş.</returns>
    protected static string SafeExtension(string name)
    {
        var extension = Path.GetExtension(name);
        if (string.IsNullOrEmpty(extension) || extension.Length > 11)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(".");
        foreach (var ch in extension.AsSpan(1))
        {
            if (!char.IsAsciiLetterOrDigit(ch))
            {
                return string.Empty;
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.Length > 1 ? builder.ToString() : string.Empty;
    }

    /// <summary>Göreli yolu mutlak yola çevirir; kök dışına çıkarsa <c>null</c>.</summary>
    /// <param name="relativePath">Göreli yol (<c>/</c> ayraçlı).</param>
    /// <returns>Mutlak yol ya da <c>null</c>.</returns>
    protected string? ResolvePath(string relativePath)
    {
        var root = Path.GetFullPath(AppData.GetPath(RootFolderName));
        var full = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ? full : null;
    }
}
