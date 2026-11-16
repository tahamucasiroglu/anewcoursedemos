using System.Globalization;
using System.Text.Json;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Dtos;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Dtos;
using Konsept.Modules.Production.Application.Resources;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Rules;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IProducedVideoService"/> (spec 0007). Video = bir konseptin tamamlanmış sürüm kayıtları (0017 <c>production.videos</c>);
/// sürüm numarası bitiş sırasıdır. Her okuma kullanıcı kimliğiyle süzülür (BR-1); başkasının kaydı "bulunamadı" ve ajana komut gitmez.
/// Dosya eylemi yalnızca sahibin ajanına gider; ajan <see cref="FileActionTimeout"/> içinde cevap vermezse zaman aşımı.
/// </summary>
/// <param name="videos">Sürüm kayıtları.</param>
/// <param name="agents">Ajan komutları.</param>
/// <param name="profiles">Profil sahipliği.</param>
/// <param name="models">Katalog (model adları).</param>
/// <param name="user">İsteği yapan kullanıcı.</param>
public abstract class ProducedVideoServiceBase(
    IProducedVideoRepository videos,
    IAgentCommands agents,
    IProfileGenerationReader profiles,
    IModelDirectory models,
    ICurrentUser user) : IProducedVideoService
{
    /// <summary>Liste parçası boyu (Req 1).</summary>
    public const int PageSize = 50;

    /// <summary>Tamamlanan sürümün durum anahtarı (0006 Req 13: "Tamamlandı").</summary>
    public const string StatusCompleted = "completed";

    /// <summary>Dosya eylemi zaman aşımı (Req 4).</summary>
    public static readonly TimeSpan FileActionTimeout = TimeSpan.FromSeconds(10);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<Result<ProducedLibraryPageDto>> ListAsync(Guid profileId, int skip, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId)
        {
            return Result<ProducedLibraryPageDto>.Failure(ProductionErrors.SignInRequired);
        }

        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<ProducedLibraryPageDto>.Failure(ProductionErrors.ProfileNotFound);
        }

        var rows = await videos.ListAsync(userId, profileId, cancellationToken).ConfigureAwait(false);
        var items = rows
            .GroupBy(v => v.ConceptId)
            .Select(g =>
            {
                var ordered = g.OrderBy(v => v.EndedAt).ToList();
                var latest = ordered[^1];
                return new ProducedLibraryItemDto(g.Key, latest.Title, latest.DurationSeconds, ordered[0].EndedAt, ordered.Count);
            })
            .OrderByDescending(i => i.ProducedAt)
            .ThenByDescending(i => i.Id)
            .ToList();
        var from = Math.Max(0, skip);
        return Result<ProducedLibraryPageDto>.Success(new ProducedLibraryPageDto([.. items.Skip(from).Take(PageSize)], items.Count > from + PageSize));
    }

    /// <inheritdoc />
    public async Task<Result<ProducedVideoDetailDto>> GetAsync(Guid videoId, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId)
        {
            return Result<ProducedVideoDetailDto>.Failure(ProductionErrors.SignInRequired);
        }

        var rows = await videos.ListByConceptAsync(userId, videoId, cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return Result<ProducedVideoDetailDto>.Failure(ProductionErrors.VideoNotFound);
        }

        var listed = await models.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var names = (listed ?? []).GroupBy(m => m.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().Name, StringComparer.Ordinal);
        string? Name(string? id) => id is null ? null : names.GetValueOrDefault(id, id);

        var versions = rows.Select((v, i) => ToDto(v, i + 1, i == 0 ? null : rows[i - 1], Name)).ToList();
        return Result<ProducedVideoDetailDto>.Success(new ProducedVideoDetailDto(videoId, rows[^1].Title, rows.Sum(v => v.CostUsd), versions));
    }

    /// <inheritdoc />
    public async Task<Result<FileActionRequest>> OpenFileAsync(Guid versionId, FileActionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (user.UserId is not { } userId)
        {
            return Result<FileActionRequest>.Failure(ProductionErrors.SignInRequired);
        }

        if (request.Action is not (AgentFileActionRequest.Folder or AgentFileActionRequest.Player))
        {
            return Result<FileActionRequest>.Failure(ProductionErrors.FileActionInvalid);
        }

        // Yalnızca sahibinin sürümü ve yalnızca sahibinin ajanı (BR-1, Req 10): başkasınınki ajana hiç gitmez.
        if (await videos.GetAsync(userId, versionId, cancellationToken).ConfigureAwait(false) is not { } video)
        {
            return Result<FileActionRequest>.Failure(ProductionErrors.VideoNotFound);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(FileActionTimeout);
        AgentFileActionReply? reply;
        try
        {
            reply = await agents.OpenFileAsync(userId, new AgentFileActionRequest(video.Id, video.FileName, request.Action), timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<FileActionRequest>.Failure(ProductionErrors.FileActionTimeout);
        }

        return reply switch
        {
            null => Result<FileActionRequest>.Failure(ProductionErrors.StreamAgentOffline),
            { Ok: true } => Result<FileActionRequest>.Success(request),
            { Error: AgentFileActionReply.NotFound } => Result<FileActionRequest>.Failure(ProductionErrors.StreamFileMissing),
            _ => Result<FileActionRequest>.Failure(ProductionErrors.FileActionFailed),
        };
    }

    private static ProducedVersionDto ToDto(ProducedVideo v, int number, ProducedVideo? previous, Func<string?, string?> name)
    {
        var content = Content(v.ConceptText);
        return new ProducedVersionDto(
            v.Id,
            number,
            StatusCompleted,
            ChangeText(VersionChanges.Describe(previous, v)),
            v.CostUsd,
            v.DurationSeconds,
            v.Format,
            v.StartedAt,
            v.EndedAt,
            name(v.CodeModel),
            name(v.Engine) ?? v.Engine,
            name(v.ImageModel),
            name(v.AudioModel),
            v.Subtitles,
            v.ProgressBar,
            v.AiVoiceNotice,
            content?.Title ?? v.Title,
            [.. (content?.Scenes ?? []).Select(s => new ProducedSceneDto(s.Title, s.Visual, s.Narration, s.DurationSeconds))]);
    }

    private static ConceptContent? Content(string text)
    {
        try
        {
            return JsonSerializer.Deserialize<ConceptContent>(text, Json);
        }
        catch (JsonException)
        {
            return null; // Bozuk kopya: sahneler boş görünür, sayfa çalışmaya devam eder.
        }
    }

    private static string ChangeText(IReadOnlyList<string> changes)
    {
        if (changes.Count == 1 && changes[0] == VersionChanges.First)
        {
            return ProductionText.ChangeFirst;
        }

        if (changes.Count == 0)
        {
            return ProductionText.ChangeNone;
        }

        var parts = changes.Select(c => c switch
        {
            VersionChanges.Concept => ProductionText.ChangeConcept,
            VersionChanges.CodeModel => ProductionText.ChangeCodeModel,
            VersionChanges.Engine => ProductionText.ChangeEngine,
            VersionChanges.ImageModel => ProductionText.ChangeImageModel,
            VersionChanges.AudioModel => ProductionText.ChangeAudioModel,
            VersionChanges.Subtitles => ProductionText.ChangeSubtitles,
            VersionChanges.ProgressBar => ProductionText.ChangeProgressBar,
            _ => ProductionText.ChangeFormat,
        });
        return string.Format(CultureInfo.InvariantCulture, ProductionText.ChangeSummary, string.Join(", ", parts));
    }
}
