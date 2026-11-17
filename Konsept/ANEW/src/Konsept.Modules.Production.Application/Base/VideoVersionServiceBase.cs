using System.Text.Json;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Dtos;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Dtos;
using Konsept.Modules.Production.Application.Rules;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Konsept.Modules.Production.Domain.Rules;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IVideoVersionService"/> (spec 0020). Sürüm işi 0006 sırasına eklenen, ayarları önceden sabitlenmiş sıradan bir iştir: onaylanan
/// aşamalar ve online onayı işte saklanır, hat yalnızca onlarla çalışır. Sıra: oturum → videonun sahibi mi (değilse "bulunamadı", iş yok, ajana
/// komut yok — BR-1) → videonun tamamlanmamış işi var mı (varsa yeni iş yok — Req 8) → seçimler katalogdan (Req 2) → önizleme (Req 3) →
/// online çağrı varsa açık onay → tekil dizinle ekleme (çift tıklama ikinci iş açmaz) → kullanıcının çalışan işi yoksa ve önünde Sırada iş
/// yoksa hemen başlar (koşucu isteği yapanın kimliğiyle), aksi halde sıranın sonunda bekler.
/// </summary>
/// <param name="videos">Sürüm kayıtları.</param>
/// <param name="jobs">İş deposu.</param>
/// <param name="outputs">Aşama çıktıları.</param>
/// <param name="concepts">Konsept köprüsü.</param>
/// <param name="regeneration">Yeniden üret bağlamı.</param>
/// <param name="models">Katalog.</param>
/// <param name="components">Kurulu bileşenler (Remotion).</param>
/// <param name="production">Konseptten Video servisi (Tekrar dene = Devam et).</param>
/// <param name="user">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
/// <param name="pipeline">Hat bağlantıları.</param>
public abstract class VideoVersionServiceBase(
    IProducedVideoRepository videos,
    IProductionJobRepository jobs,
    IStageOutputRepository outputs,
    IConceptQueueBridge concepts,
    IConceptRegenerationContext regeneration,
    IModelDirectory models,
    IInstalledComponentsReader components,
    IProductionService production,
    ICurrentUser user,
    TimeProvider time,
    ProductionPipelineLinks pipeline) : IVideoVersionService
{
    /// <summary>Yarım dosya silmede ajanın cevap süresi sınırı.</summary>
    public static readonly TimeSpan RemoveTimeout = TimeSpan.FromSeconds(30);

    private static readonly ProductionJobStatus[] Running = [ProductionJobStatus.Producing, ProductionJobStatus.WaitingForAgent];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<Result<VersionOptionsDto>> GetOptionsAsync(Guid videoId, Guid? baseVersionId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(videoId, baseVersionId, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return Result<VersionOptionsDto>.Failure(loaded.Error!);
        }

        var context = loaded.Value;
        var listed = await ListedAsync(context.UserId, cancellationToken).ConfigureAwait(false);
        if (listed is null)
        {
            return Result<VersionOptionsDto>.Failure(ProductionErrors.ModelsUnavailable);
        }

        var s = context.Settings;
        return Result<VersionOptionsDto>.Success(new VersionOptionsDto(
            context.Base.Id,
            s.Engine,
            s.ImageModel,
            s.AudioModel,
            s.Subtitles,
            s.ProgressBar,
            Options(listed, ModelInfo.KindVideoEngine),
            Options(listed, ModelInfo.KindImage),
            Options(listed, ModelInfo.KindAudio)));
    }

    /// <inheritdoc />
    public async Task<Result<VersionPreviewDto>> PreviewAsync(Guid videoId, NewVersionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loaded = await LoadAsync(videoId, request.BaseVersionId, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return Result<VersionPreviewDto>.Failure(loaded.Error!);
        }

        var next = await NextAsync(loaded.Value, request, cancellationToken).ConfigureAwait(false);
        if (!next.IsSuccess)
        {
            return Result<VersionPreviewDto>.Failure(next.Error!);
        }

        var (stages, online) = await PlanAsync(loaded.Value, next.Value, cancellationToken).ConfigureAwait(false);
        return Result<VersionPreviewDto>.Success(new VersionPreviewDto([.. stages.Select(JobNames.Stage)], online, stages.Count == 0));
    }

    /// <inheritdoc />
    public async Task<Result<ActiveVersionDto>> StartAsync(Guid videoId, NewVersionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loaded = await LoadAsync(videoId, request.BaseVersionId, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return Result<ActiveVersionDto>.Failure(loaded.Error!);
        }

        var context = loaded.Value;
        if (ActiveError(context) is { } busy)
        {
            return Result<ActiveVersionDto>.Failure(busy);
        }

        var next = await NextAsync(context, request, cancellationToken).ConfigureAwait(false);
        if (!next.IsSuccess)
        {
            return Result<ActiveVersionDto>.Failure(next.Error!);
        }

        var (stages, online) = await PlanAsync(context, next.Value, cancellationToken).ConfigureAwait(false);
        if (stages.Count == 0)
        {
            return Result<ActiveVersionDto>.Failure(ProductionErrors.VersionNoChange);
        }

        if (online && !request.ConfirmOnline)
        {
            return Result<ActiveVersionDto>.Failure(ProductionErrors.VersionOnlineConfirm); // Onaysız iş yok, çağrı yok (Req 3, H-3).
        }

        return await AddAsync(context, VersionKind.NewVersion, next.Value, stages, online).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<RegenerationContextDto>> OpenRegenerationAsync(Guid videoId, Guid? baseVersionId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(videoId, baseVersionId, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return Result<RegenerationContextDto>.Failure(loaded.Error!);
        }

        var context = loaded.Value;
        if (ActiveError(context) is { } busy)
        {
            return Result<RegenerationContextDto>.Failure(busy);
        }

        var token = regeneration.Open(context.UserId, videoId);
        return Result<RegenerationContextDto>.Success(new RegenerationContextDto(token, videoId, context.Base.ProfileId, context.Base.Id));
    }

    /// <inheritdoc />
    public async Task<Result<VersionPreviewDto>> PreviewRegenerationAsync(Guid videoId, string? token, Guid? baseVersionId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(videoId, baseVersionId, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return Result<VersionPreviewDto>.Failure(loaded.Error!);
        }

        var context = loaded.Value;
        if (!regeneration.IsOpen(context.UserId, videoId, token))
        {
            return Result<VersionPreviewDto>.Failure(ProductionErrors.RegenerateContextInvalid);
        }

        // Konsept metni her durumda online LLM'le koda dönüşür: tüm aşamalar, online çağrı (Req 4).
        return Result<VersionPreviewDto>.Success(new VersionPreviewDto(
            [.. VersionStages.All(context.Settings).Select(JobNames.Stage)], true, Unchanged(context)));
    }

    /// <inheritdoc />
    public async Task<Result<ActiveVersionDto>> StartRegenerationAsync(Guid videoId, RegenerateVersionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loaded = await LoadAsync(videoId, request.BaseVersionId, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return Result<ActiveVersionDto>.Failure(loaded.Error!);
        }

        var context = loaded.Value;
        if (!regeneration.IsOpen(context.UserId, videoId, request.Token))
        {
            return Result<ActiveVersionDto>.Failure(ProductionErrors.RegenerateContextInvalid);
        }

        if (ActiveError(context) is { } busy)
        {
            return Result<ActiveVersionDto>.Failure(busy);
        }

        if (!request.Force && Unchanged(context))
        {
            return Result<ActiveVersionDto>.Failure(ProductionErrors.RegenerateUnchanged); // "Yine de baştan üret" seçilmedi (Req 5).
        }

        if (!request.ConfirmOnline)
        {
            return Result<ActiveVersionDto>.Failure(ProductionErrors.VersionOnlineConfirm);
        }

        var settings = context.Settings with { FixedAt = time.GetUtcNow() };
        var result = await AddAsync(context, VersionKind.Regenerate, settings, VersionStages.All(settings), true).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            regeneration.Close(context.UserId, videoId); // Sıradaki konsept düzenlenmez.
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<ActiveVersionDto>> RetryAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(videoId, null, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return Result<ActiveVersionDto>.Failure(loaded.Error!);
        }

        if (loaded.Value.Active is not { Status: ProductionJobStatus.Failed, Version: not null } failed)
        {
            return Result<ActiveVersionDto>.Failure(ProductionErrors.VersionNotFailed);
        }

        // Devam et ile aynı yol (0006 Req 15, 0017 Req 9): tamamlanan aşamalar yeniden çalışmaz.
        var resumed = await production.ResumeAsync(failed.Id, cancellationToken).ConfigureAwait(false);
        if (!resumed.IsSuccess)
        {
            return Result<ActiveVersionDto>.Failure(resumed.Error!);
        }

        var job = await jobs.GetAsync(loaded.Value.UserId, failed.Id, CancellationToken.None).ConfigureAwait(false);
        return Result<ActiveVersionDto>.Success(JobNames.Active(job ?? failed));
    }

    /// <inheritdoc />
    public async Task<Result<ActiveVersionDto>> RemoveFailedAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(videoId, null, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return Result<ActiveVersionDto>.Failure(loaded.Error!);
        }

        var userId = loaded.Value.UserId;
        if (loaded.Value.Active is not { Status: ProductionJobStatus.Failed, Version: not null } failed)
        {
            return Result<ActiveVersionDto>.Failure(ProductionErrors.VersionNotFailed);
        }

        if (failed.Progress?.PendingFile is { } file && await DeletePartialFileAsync(userId, videoId, file, cancellationToken).ConfigureAwait(false) is { } fileError)
        {
            return Result<ActiveVersionDto>.Failure(fileError); // Kayıt kalır, tekrar denenebilir.
        }

        // Yarım aşamaların çıktısı geçersiz: sonraki sürüm onları yeniden üretir (önizleme gösterir).
        var unfinished = (failed.Progress?.Stages ?? []).Where(s => s.State is not (StageState.Done or StageState.Skipped)).Select(s => s.Stage).ToList();
        if (await outputs.GetAsync(userId, videoId, CancellationToken.None).ConfigureAwait(false) is { } stored && unfinished.Count > 0)
        {
            var drop = unfinished.SelectMany(s => VersionStages.Dependents(s).Append(s)).ToHashSet();
            await outputs.UpsertAsync(
                stored with
                {
                    Signatures = stored.Signatures.Where(p => !drop.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value),
                    LocalDone = [.. stored.LocalDone.Where(s => !drop.Contains(s))],
                    UpdatedAt = time.GetUtcNow(),
                },
                CancellationToken.None).ConfigureAwait(false);
        }

        if (!await jobs.RemoveAsync(userId, failed.Id, [ProductionJobStatus.Failed], CancellationToken.None).ConfigureAwait(false))
        {
            return Result<ActiveVersionDto>.Failure(ProductionErrors.VersionNotFailed);
        }

        return Result<ActiveVersionDto>.Success(JobNames.Active(failed));
    }

    private static List<VersionOptionDto> Options(IReadOnlyList<ModelInfo> listed, string kind) =>
        [.. listed.Where(m => string.Equals(m.Kind, kind, StringComparison.Ordinal)).Select(m => new VersionOptionDto(m.Id, m.Name, m.Local))];

    private static Error? ActiveError(VersionContext context) => context.Active switch
    {
        null => null,
        { Status: ProductionJobStatus.Failed } => ProductionErrors.VersionFailedPending,
        _ => ProductionErrors.VersionActive, // Sırada · Üretiliyor · Durduruldu · Ajan bekleniyor (Req 8).
    };

    private static bool Unchanged(VersionContext context)
    {
        ConceptContent? previous;
        try
        {
            previous = JsonSerializer.Deserialize<ConceptContent>(context.Base.ConceptText, Json);
        }
        catch (JsonException)
        {
            previous = null; // Bozuk kopya: değişmiş sayılır.
        }

        return previous is not null && PipelineHashes.SameText(previous, context.Content);
    }

    private static Error? Pick(IReadOnlyList<ModelInfo> all, IReadOnlyList<ModelInfo> listed, string? id, string kind)
    {
        if (listed.Any(m => string.Equals(m.Id, id, StringComparison.Ordinal) && string.Equals(m.Kind, kind, StringComparison.Ordinal)))
        {
            return null;
        }

        // Yetkisiz model listelenmez ve istekte reddedilir (BR-13); listede olmayan (Ollama, kurulu olmayan Remotion, bilinmeyen) da.
        return all.Any(m => string.Equals(m.Id, id, StringComparison.Ordinal) && m.Availability == ModelInfo.NoPermission)
            ? ProductionErrors.NoPermission
            : ProductionErrors.ModelNotListed;
    }

    private async Task<Error?> DeletePartialFileAsync(Guid userId, Guid videoId, string file, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RemoveTimeout);
        AgentDeleteFilesReply? reply;
        try
        {
            reply = await pipeline.Agents.DeleteFilesAsync(userId, new AgentDeleteFilesRequest(videoId, [file]), timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProductionErrors.FileActionTimeout;
        }

        return reply switch
        {
            null => ProductionErrors.VersionRemoveAgentOffline,
            { Remaining: { Count: 0 } } => null,
            _ => ProductionErrors.VersionRemovePartial,
        };
    }

    private async Task<Result<FixedJobSettings>> NextAsync(VersionContext context, NewVersionRequest request, CancellationToken cancellationToken)
    {
        var current = context.Settings;
        var engine = string.IsNullOrWhiteSpace(request.Engine) ? current.Engine : request.Engine;
        var image = string.IsNullOrWhiteSpace(request.ImageModel) ? null : request.ImageModel;
        var audio = string.IsNullOrWhiteSpace(request.AudioModel) ? null : request.AudioModel;
        if ((current.ImageModel is null) != (image is null))
        {
            return Result<FixedJobSettings>.Failure(ProductionErrors.VersionImageToggle);
        }

        var changed = new List<(string? Id, string Kind)>();
        if (!string.Equals(engine, current.Engine, StringComparison.Ordinal))
        {
            changed.Add((engine, ModelInfo.KindVideoEngine));
        }

        if (!string.Equals(image, current.ImageModel, StringComparison.Ordinal))
        {
            changed.Add((image, ModelInfo.KindImage));
        }

        if (audio is not null && !string.Equals(audio, current.AudioModel, StringComparison.Ordinal))
        {
            changed.Add((audio, ModelInfo.KindAudio));
        }

        if (changed.Count > 0)
        {
            // Yalnızca değişen seçimler doğrulanır: tabanın, yetkisi sonradan kalkan seçimi korunabilir.
            var all = await models.ListAllAsync(cancellationToken).ConfigureAwait(false);
            var listed = await ListedAsync(context.UserId, cancellationToken).ConfigureAwait(false);
            if (all is null || listed is null)
            {
                return Result<FixedJobSettings>.Failure(ProductionErrors.ModelsUnavailable);
            }

            foreach (var (id, kind) in changed)
            {
                if (Pick(all, listed, id, kind) is { } error)
                {
                    return Result<FixedJobSettings>.Failure(error);
                }
            }
        }

        return Result<FixedJobSettings>.Success(current with
        {
            Engine = engine!,
            ImageModel = image,
            AudioModel = audio,
            Subtitles = request.Subtitles && audio is not null, // Sessiz videoda altyazı yok (0006 Req 5).
            ProgressBar = request.ProgressBar,
            FixedAt = time.GetUtcNow(),
        });
    }

    /// <summary>
    /// Çalışacak aşamalar (Req 1, 3): tablo ∪ çıktısı bu ayarlarla geçerli olmayanlar (imza; yerelde aynı cihaz); hiçbir ayar değişmediyse boş.
    /// </summary>
    private async Task<(IReadOnlyList<PipelineStage> Stages, bool Online)> PlanAsync(VersionContext context, FixedJobSettings next, CancellationToken cancellationToken)
    {
        var affected = VersionStages.Affected(context.Settings, next);
        if (affected.Count == 0)
        {
            return ([], false);
        }

        var stored = PipelineHashes.WithLegacySignatures(
            await outputs.GetAsync(context.UserId, context.Base.ConceptId, cancellationToken).ConfigureAwait(false), context.Content, context.Settings);
        var device = pipeline.Agents.ConnectedDevice(context.UserId) ?? stored?.LocalDevice;
        var stages = VersionStages.RunSet(next, stored, PipelineHashes.Signatures(context.Content, next), device, affected);
        return (stages, VersionStages.Online(stages, next));
    }

    private async Task<Result<ActiveVersionDto>> AddAsync(
        VersionContext context, VersionKind kind, FixedJobSettings settings, IReadOnlyList<PipelineStage> stages, bool online)
    {
        var userId = context.UserId;
        var now = time.GetUtcNow();
        var position = await jobs.MaxPositionAsync(userId, context.Base.ProfileId, CancellationToken.None).ConfigureAwait(false);
        var job = new ProductionJob
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            ProfileId = context.Base.ProfileId,
            ConceptId = context.Base.ConceptId,
            Title = context.Content.Title,
            Language = settings.Language,
            Format = settings.Format,
            TargetDurationSeconds = settings.TargetDurationSeconds,
            Position = position + 1, // Sıranın sonu (Req 6).
            Status = ProductionJobStatus.Queued,
            FixedSettings = settings, // Önceden sabit: profil ayarı kullanılmaz.
            Version = new VersionRequest { Kind = kind, BaseVideoId = context.Base.Id, BaseSettings = context.Settings, Stages = stages, Online = online },
            CreatedAt = now,
            UpdatedAt = now,
        };
        if (!await jobs.TryAddAsync(job, CancellationToken.None).ConfigureAwait(false))
        {
            return Result<ActiveVersionDto>.Failure(ProductionErrors.VersionActive); // Çift tıklama / yeniden gönderim (Req 8).
        }

        await StartIfIdleAsync(userId, job).ConfigureAwait(false);
        var saved = await jobs.GetAsync(userId, job.Id, CancellationToken.None).ConfigureAwait(false);
        return Result<ActiveVersionDto>.Success(JobNames.Active(saved ?? job));
    }

    /// <summary>Kullanıcının çalışan işi yoksa ve profil sırasında önünde Sırada iş yoksa sürüm işi hemen başlar (plan 0020 Risk 4).</summary>
    private async Task StartIfIdleAsync(Guid userId, ProductionJob job)
    {
        var all = await jobs.ListActiveAsync(userId, CancellationToken.None).ConfigureAwait(false);
        if (all.Any(j => Running.Contains(j.Status))
            || all.Any(j => j.ProfileId == job.ProfileId && j.Status == ProductionJobStatus.Queued && j.Position < job.Position))
        {
            return;
        }

        var now = time.GetUtcNow();
        if (!await jobs.StartAsync(userId, job.Id, [ProductionJobStatus.Queued], job.FixedSettings!, now, CancellationToken.None).ConfigureAwait(false))
        {
            return; // Bu arada başka iş başladı: sürüm işi sırada bekler.
        }

        await pipeline.Queue.SetAsync(new QueueState { UserId = userId, UpdatedAt = now }, CancellationToken.None).ConfigureAwait(false);
        if (pipeline.Caller.Current() is { } caller)
        {
            pipeline.Runner.Launch(userId, caller);
        }
    }

    private async Task<IReadOnlyList<ModelInfo>?> ListedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var all = await models.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var installed = components.Get(userId);
        return all?.Where(m => m.Availability != ModelInfo.NoPermission && m.Kind switch
        {
            ModelInfo.KindImage or ModelInfo.KindAudio => true,
            ModelInfo.KindVideoEngine => m.Component is null || installed.Contains(m.Component), // Remotion yalnızca kuruluysa (0009).
            _ => false, // Metin modelleri (Ollama dahil) burada seçilmez (E-4).
        }).ToList();
    }

    private async Task<Result<VersionContext>> LoadAsync(Guid videoId, Guid? baseVersionId, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId)
        {
            return Result<VersionContext>.Failure(ProductionErrors.SignInRequired);
        }

        // Yalnızca sahibinin silinmemiş videosu (BR-1, Req 14): başkasınınki "bulunamadı", iş ve ajan komutu yok.
        var rows = await videos.ListByConceptAsync(userId, videoId, cancellationToken).ConfigureAwait(false);
        var chosen = baseVersionId is { } id && id != Guid.Empty ? rows.FirstOrDefault(v => v.Id == id) : rows.LastOrDefault();
        if (chosen is null)
        {
            return Result<VersionContext>.Failure(ProductionErrors.VideoNotFound);
        }

        if (await concepts.GetContentAsync(userId, videoId, cancellationToken).ConfigureAwait(false) is not { } content)
        {
            return Result<VersionContext>.Failure(ProductionErrors.VideoNotFound);
        }

        var baseJob = await jobs.GetAsync(userId, chosen.JobId, cancellationToken).ConfigureAwait(false);
        var settings = baseJob?.FixedSettings ?? new FixedJobSettings
        {
            Subtitles = chosen.Subtitles,
            ProgressBar = chosen.ProgressBar,
            CodeModel = chosen.CodeModel,
            Engine = chosen.Engine,
            ImageModel = chosen.ImageModel,
            AudioModel = chosen.AudioModel,
            Language = content.Language,
            Format = chosen.Format,
            TargetDurationSeconds = chosen.DurationSeconds,
            FixedAt = chosen.StartedAt,
        };
        var active = (await jobs.ListActiveAsync(userId, cancellationToken).ConfigureAwait(false)).FirstOrDefault(j => j.ConceptId == videoId);
        return Result<VersionContext>.Success(new VersionContext(userId, rows, chosen, settings, content, active));
    }
}
