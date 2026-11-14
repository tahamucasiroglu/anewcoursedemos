using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Modules.Agents.Domain.Errors;
using TahaKonsept.Modules.Agents.Infrastructure.Storage.Base;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Infrastructure.Storage.Concrete;

/// <summary>
/// Dosya sistemi artefakt deposu: dosyalar <c>App_Data/artifacts/{jobId:N}/{artifactId:N}{ext}</c>, kayıtlar <c>agents.stored_artifacts</c>.
/// Silmede önce kayıt, kayıt başarılıysa dosya kaldırılır; boşalan iş klasörü yalnız işin artefaktları toplu silinince temizlenir
/// (tek artefakt onayı aynı işin o an yazılan dosyasının klasörünü silmesin; yazma klasörü kaybolursa bir kez yeniden dener). Scoped.
/// </summary>
public sealed class FileSystemArtifactStore : ArtifactStoreBase
{
    private readonly IStoredArtifactRepository _artifacts;

    /// <summary>Oluşturur.</summary>
    /// <param name="appData">Uygulama veri klasörü.</param>
    /// <param name="artifacts">Kayıt deposu.</param>
    public FileSystemArtifactStore(IAppDataLocation appData, IStoredArtifactRepository artifacts)
        : base(appData)
    {
        _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
    }

    /// <summary>Kaydeder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="name">Ad.</param>
    /// <param name="contentType">İçerik türü.</param>
    /// <param name="content">İçerik.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İndirme bilgisi.</returns>
    public override async Task<Result<ArtifactDownload>> SaveAsync(
        Guid userId,
        Guid jobId,
        string name,
        string? contentType,
        Stream content,
        int? sceneIndex = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!IsValidName(name))
        {
            return AgentErrors.ArtifactNameInvalid;
        }

        var id = Guid.CreateVersion7();
        var jobFolder = jobId.ToString("N");
        var fileName = id.ToString("N") + SafeExtension(name);
        long size;
        await using (var file = CreateFile(jobFolder, fileName))
        {
            await content.CopyToAsync(file, cancellationToken);
            size = file.Length;
        }

        var artifact = StoredArtifact.Create(id, userId, jobId, name, contentType, jobFolder + "/" + fileName, size, sceneIndex);
        await _artifacts.AddAsync(artifact, cancellationToken);
        await _artifacts.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new ArtifactDownload
        {
            ArtifactId = id,
            Name = name,
            Url = DownloadUrl(id),
            ContentType = artifact.ContentType,
            SceneIndex = sceneIndex,
        };
    }

    /// <summary>Açar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="artifactId">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İçerik.</returns>
    public override async Task<Result<ArtifactContent>> OpenAsync(Guid userId, Guid artifactId, CancellationToken cancellationToken = default)
    {
        var artifact = await _artifacts.GetByIdAsync(artifactId, cancellationToken);
        if (artifact is null || artifact.UserId != userId)
        {
            return AgentErrors.ArtifactNotFound;
        }

        var path = ResolvePath(artifact.RelativePath);
        if (path is null || !File.Exists(path))
        {
            return AgentErrors.ArtifactNotFound;
        }

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return new ArtifactContent(stream, artifact.ContentType, artifact.Name, stream.Length);
    }

    /// <summary>Siler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="artifactId">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override async Task<Result> DeleteAsync(Guid userId, Guid artifactId, CancellationToken cancellationToken = default)
    {
        var artifact = await _artifacts.GetByIdAsync(artifactId, cancellationToken);
        if (artifact is null || artifact.UserId != userId)
        {
            return AgentErrors.ArtifactNotFound;
        }

        // Önce kayıt silinir, sonra dosya: kayıt başarısız olursa DB satırı eksik dosyayı göstermez. Boş iş klasörü burada silinmez
        // (aynı işin başka artefaktı o an yazılıyor olabilir); klasörü işin tüm artefaktlarını silen DeleteJobArtifactsAsync temizler.
        _artifacts.Remove(artifact);
        await _artifacts.UnitOfWork.SaveChangesAsync(cancellationToken);
        DeleteFile(artifact);
        return Result.Success();
    }

    /// <summary>İşin artefaktlarını siler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen sayısı.</returns>
    public override async Task<Result<int>> DeleteJobArtifactsAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var artifacts = await _artifacts.ListByJobAsync(userId, jobId, cancellationToken);
        foreach (var artifact in artifacts)
        {
            _artifacts.Remove(artifact);
        }

        if (artifacts.Count > 0)
        {
            await _artifacts.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        foreach (var artifact in artifacts)
        {
            DeleteFile(artifact);
        }

        DeleteJobFolderIfEmpty(jobId);
        return artifacts.Count;
    }

    private FileStream CreateFile(string jobFolder, string fileName)
    {
        // Klasör, dosya açılmadan hemen önce oluşturulur; arada başka bir temizlik silerse bir kez yeniden oluşturulup denenir.
        for (var attempt = 0; ; attempt++)
        {
            var path = Path.Combine(AppData.EnsureDirectory(RootFolderName, jobFolder), fileName);
            try
            {
                return new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            }
            catch (DirectoryNotFoundException) when (attempt == 0)
            {
                // Klasör EnsureDirectory ile açılış arasında silindi: döngü yeniden oluşturup bir kez daha dener.
            }
        }
    }

    private void DeleteFile(StoredArtifact artifact)
    {
        var path = ResolvePath(artifact.RelativePath);
        if (path is not null && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void DeleteJobFolderIfEmpty(Guid jobId)
    {
        var folder = AppData.GetPath(RootFolderName, jobId.ToString("N"));
        try
        {
            if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
            {
                Directory.Delete(folder);
            }
        }
        catch (IOException)
        {
            // Bu arada klasöre yeni dosya yazıldı (boş değil) ya da başka işlem sildi: klasör kalsın.
        }
    }
}
