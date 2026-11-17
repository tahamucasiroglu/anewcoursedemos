using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Enums;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Dtos;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Konsept.Modules.Production.Domain.Rules;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Production.Application.Base;

/// <summary>
/// Konseptten Video'nun kuralları (spec 0006): ayarlar profil başına (yoksa varsayılanlar, görsel varsayılanı kurulu bileşene göre),
/// seçimler katalogdan doğrulanır (BR-13; Remotion yalnızca kuruluysa listelenir), sıraya alma yalnızca <i>Onaylandı</i> konseptten ve
/// hepsi-ya-hiç, taşıma/çıkarma çalışan işte reddedilir, kullanıcı başına tek çalışan iş, iş başlarken ayarlar ve konseptin kaydındaki
/// değerler işe sabitlenir. Konsept durumları Concepts.Contracts ile değişir. Başkasının profili/işi 404 (BR-1).
/// </summary>
/// <param name="jobs">İş deposu.</param>
/// <param name="settingsStore">Ayar deposu.</param>
/// <param name="concepts">Konsept köprüsü.</param>
/// <param name="profiles">Profil okuyucu (sahiplik).</param>
/// <param name="models">Katalog dizini.</param>
/// <param name="components">Kurulu bileşenler.</param>
/// <param name="user">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
/// <param name="pipeline">Hat bağlantıları (spec 0017): koşucu, çağıran kimliği, ajan, video kayıtları, sıra durumu.</param>
public abstract class ProductionServiceBase(
    IProductionJobRepository jobs,
    IProductionSettingsRepository settingsStore,
    IConceptQueueBridge concepts,
    IProfileGenerationReader profiles,
    IModelDirectory models,
    IInstalledComponentsReader components,
    ICurrentUser user,
    TimeProvider time,
    ProductionPipelineLinks pipeline) : IProductionService
{
    /// <summary>Bir istekte sıraya alınabilecek en çok konsept.</summary>
    public const int MaxEnqueue = 500;

    private static readonly ProductionJobStatus[] Running = [ProductionJobStatus.Producing, ProductionJobStatus.WaitingForAgent];
    private static readonly ProductionJobStatus[] Movable = [ProductionJobStatus.Queued, ProductionJobStatus.Stopped, ProductionJobStatus.Failed];
    private static readonly ProductionJobStatus[] Resumable = [ProductionJobStatus.Stopped, ProductionJobStatus.Failed];

    /// <inheritdoc />
    public Task<Result<ProductionStateDto>> GetAsync(Guid profileId, CancellationToken cancellationToken) =>
        WithProfileAsync(profileId, (userId, _) => Task.FromResult<Error?>(null), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ProductionStateDto>> SaveSettingsAsync(Guid profileId, SaveProductionSettingsRequest request, CancellationToken cancellationToken) =>
        WithProfileAsync(
            profileId,
            async (userId, ct) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var listed = await ListedAsync(userId, ct).ConfigureAwait(false);
                if (listed is null)
                {
                    return ProductionErrors.ModelsUnavailable;
                }

                var current = await CurrentSettingsAsync(userId, profileId, ct).ConfigureAwait(false);
                var code = Check(listed, request.CodeModel, ModelInfo.KindText, current.CodeModel, out var codeError);
                var engine = Check(listed, request.Engine ?? ProductionDefaults.Engine, ModelInfo.KindVideoEngine, current.Engine, out var engineError);
                var image = Check(listed, request.ImageModel, ModelInfo.KindImage, current.ImageModel, out var imageError);
                var audio = Check(listed, request.AudioModel, ModelInfo.KindAudio, current.AudioModel, out var audioError);
                if ((codeError ?? engineError ?? imageError ?? audioError) is { } error)
                {
                    return error;
                }

                await settingsStore.UpsertAsync(
                    new ProductionSettings
                    {
                        UserId = userId,
                        ProfileId = profileId,
                        Subtitles = request.Subtitles,
                        ProgressBar = request.ProgressBar,
                        CodeModel = code?.Id,
                        CodeEffort = Effort(code, request.CodeEffort),
                        Engine = engine!.Id,
                        ImageModel = image?.Id,
                        AudioModel = audio?.Id,
                        UpdatedAt = time.GetUtcNow(),
                    },
                    CancellationToken.None).ConfigureAwait(false);
                return null;
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<ProductionStateDto>> EnqueueAsync(EnqueueRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return WithProfileAsync(
            request.ProfileId,
            async (userId, ct) =>
            {
                var ids = (request.ConceptIds ?? []).Distinct().ToList();
                if (ids.Count == 0 || ids.Count > MaxEnqueue)
                {
                    return ProductionErrors.NothingSelected;
                }

                var found = (await concepts.FindAsync(userId, ids, ct).ConfigureAwait(false)).ToDictionary(c => c.Id);
                if (ids.Any(id => !found.TryGetValue(id, out var c) || c.ProfileId != request.ProfileId || c.Stage != ConceptQueueStage.Approved))
                {
                    return ProductionErrors.NotApproved; // Hepsi ya da hiçbiri (plan 0006 Risk 5): sıra ve konsept durumu değişmez.
                }

                var position = await jobs.MaxPositionAsync(userId, request.ProfileId, ct).ConfigureAwait(false);
                foreach (var id in ids)
                {
                    // Koşullu geçiş: aynı konsept iki kez sıraya giremez (eşzamanlı istekte yalnızca biri kazanır).
                    if (!await concepts.ChangeAsync(userId, id, ConceptQueueStage.Approved, ConceptQueueStage.Queued, CancellationToken.None).ConfigureAwait(false))
                    {
                        continue;
                    }

                    var concept = found[id];
                    var now = time.GetUtcNow();
                    await jobs.AddAsync(
                        new ProductionJob
                        {
                            Id = Guid.CreateVersion7(now),
                            UserId = userId,
                            ProfileId = request.ProfileId,
                            ConceptId = id,
                            Title = concept.Title,
                            Language = concept.Language,
                            Format = concept.Format,
                            TargetDurationSeconds = concept.TargetDurationSeconds,
                            Position = ++position,
                            Status = ProductionJobStatus.Queued,
                            CreatedAt = now,
                            UpdatedAt = now,
                        },
                        CancellationToken.None).ConfigureAwait(false);
                }

                return null;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ProductionStateDto>> MoveAsync(Guid jobId, MoveJobRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await WithJobAsync(
            jobId,
            async (userId, job, ct) =>
            {
                var queue = (await jobs.ListActiveAsync(userId, ct).ConfigureAwait(false))
                    .Where(j => j.ProfileId == job.ProfileId)
                    .OrderBy(j => j.Position)
                    .ToList();
                var index = queue.FindIndex(j => j.Id == job.Id);
                var other = request.Direction switch
                {
                    MoveJobRequest.Up when index > 0 => queue[index - 1],
                    MoveJobRequest.Down when index >= 0 && index < queue.Count - 1 => queue[index + 1],
                    _ => null,
                };
                if (other is null)
                {
                    return ProductionErrors.CannotMove;
                }

                if (Running.Contains(job.Status) || Running.Contains(other.Status))
                {
                    return ProductionErrors.JobLocked;
                }

                return await jobs.SwapAsync(userId, job, other, Movable, CancellationToken.None).ConfigureAwait(false) ? null : ProductionErrors.JobLocked;
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<ProductionStateDto>> RemoveAsync(Guid jobId, CancellationToken cancellationToken) =>
        WithJobAsync(
            jobId,
            async (userId, job, _) =>
            {
                if (!await jobs.RemoveAsync(userId, job.Id, Movable, CancellationToken.None).ConfigureAwait(false))
                {
                    return ProductionErrors.JobLocked;
                }

                // Konsept Onaylandı'ya döner (Req 11): durdurulmuş işin konsepti Üretiliyor, sıradakinin Sırada.
                if (!await concepts.ChangeAsync(userId, job.ConceptId, ConceptQueueStage.Queued, ConceptQueueStage.Approved, CancellationToken.None).ConfigureAwait(false))
                {
                    await concepts.ChangeAsync(userId, job.ConceptId, ConceptQueueStage.Producing, ConceptQueueStage.Approved, CancellationToken.None).ConfigureAwait(false);
                }

                return null;
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<ProductionStateDto>> StartAsync(ProfileCommandRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return WithProfileAsync(
            request.ProfileId,
            async (userId, ct) =>
            {
                var state = await BuildStateAsync(userId, request.ProfileId, ct).ConfigureAwait(false);
                if (state.StartBlock is { } reason)
                {
                    return ProductionErrors.StartBlocked(reason);
                }

                var settings = await CurrentSettingsAsync(userId, request.ProfileId, ct).ConfigureAwait(false);
                var first = (await jobs.ListActiveAsync(userId, ct).ConfigureAwait(false))
                    .Where(j => j.ProfileId == request.ProfileId && j.Status == ProductionJobStatus.Queued)
                    .OrderBy(j => j.Position)
                    .First();
                return await StartJobAsync(userId, first, settings, [ProductionJobStatus.Queued]).ConfigureAwait(false) ?? await LaunchAsync(userId).ConfigureAwait(false);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<ProductionStateDto>> StopAsync(ProfileCommandRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return WithProfileAsync(
            request.ProfileId,
            async (userId, _) =>
            {
                if (await jobs.StopAsync(userId, request.ProfileId, time.GetUtcNow(), CancellationToken.None).ConfigureAwait(false) == 0)
                {
                    return ProductionErrors.NothingRunning;
                }

                pipeline.Runner.Cancel(userId); // Süren yerel aşama kesilir; süren online çağrı tamamlanır (spec 0017 Req 9).
                return null;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<ProductionStateDto>> ResumeAsync(Guid jobId, CancellationToken cancellationToken) =>
        WithJobAsync(
            jobId,
            async (userId, job, ct) =>
            {
                if (!Resumable.Contains(job.Status))
                {
                    return ProductionErrors.CannotResume;
                }

                var all = await jobs.ListActiveAsync(userId, ct).ConfigureAwait(false);
                if (all.FirstOrDefault(j => Running.Contains(j.Status)) is { } running)
                {
                    return ProductionErrors.StartBlocked(running.ProfileId == job.ProfileId ? StartBlock.RunningHere : StartBlock.RunningElsewhere);
                }

                var settings = await CurrentSettingsAsync(userId, job.ProfileId, ct).ConfigureAwait(false);
                return await StartJobAsync(userId, job, settings, Resumable).ConfigureAwait(false) ?? await LaunchAsync(userId).ConfigureAwait(false);
            },
            cancellationToken);

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProducedVideoDto>>> ListVideosAsync(Guid profileId, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId)
        {
            return Result<IReadOnlyList<ProducedVideoDto>>.Failure(ProductionErrors.SignInRequired);
        }

        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<IReadOnlyList<ProducedVideoDto>>.Failure(ProductionErrors.ProfileNotFound);
        }

        var list = await pipeline.Videos.ListAsync(userId, profileId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<ProducedVideoDto>>.Success([.. list.Select(v => new ProducedVideoDto(v.Id, v.Title, v.EndedAt))]);
    }

    /// <inheritdoc />
    public async Task<Result<VideoStreamDto>> OpenStreamAsync(Guid videoId, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId)
        {
            return Result<VideoStreamDto>.Failure(ProductionErrors.SignInRequired);
        }

        // Yalnızca sahibinin videosu ve yalnızca sahibinin ajanı (BR-1, Req 21).
        if (await pipeline.Videos.GetAsync(userId, videoId, cancellationToken).ConfigureAwait(false) is not { } video)
        {
            return Result<VideoStreamDto>.Failure(ProductionErrors.VideoNotFound);
        }

        var reply = await pipeline.Agents.OpenStreamAsync(userId, new AgentStreamRequest(video.Id, video.FileName), cancellationToken).ConfigureAwait(false);
        return reply switch
        {
            null => Result<VideoStreamDto>.Failure(ProductionErrors.StreamAgentOffline),
            { Url: { } url } => Result<VideoStreamDto>.Success(new VideoStreamDto(url)),
            _ => Result<VideoStreamDto>.Failure(ProductionErrors.StreamFileMissing),
        };
    }

    private static StageDto ToDto(StageProgress s) => new(
        s.Stage switch
        {
            PipelineStage.Analysis => "analysis",
            PipelineStage.Code => "code",
            PipelineStage.Images => "images",
            PipelineStage.Voice => "voice",
            PipelineStage.Render => "render",
            _ => "merge",
        },
        s.State switch
        {
            StageState.Running => "running",
            StageState.Done => "done",
            StageState.Skipped => "skipped",
            StageState.Failed => "failed",
            _ => "pending",
        },
        s.Device);

    private static ModelInfo? Check(IReadOnlyList<ModelInfo> listed, string? id, string kind, string? current, out Error? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var model = listed.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.Ordinal) && string.Equals(m.Kind, kind, StringComparison.Ordinal));
        if (model is null)
        {
            error = ProductionErrors.ModelNotListed;
        }
        else if (model.Availability == ModelInfo.NoPermission && !string.Equals(id, current, StringComparison.Ordinal))
        {
            error = ProductionErrors.NoPermission; // Yetkisiz model seçilemez (AC-4); yetkisi sonradan kalkan kayıtlı seçim korunur, neden gösterilir.
        }

        return model;
    }

    private static string? Effort(ModelInfo? model, string? asked)
    {
        if (model is null || model.EffortLevels.Count == 0)
        {
            return null;
        }

        // Desteklenmeyen efor modelin varsayılanına döner (AC-2).
        return asked is not null && model.EffortLevels.Contains(asked, StringComparer.Ordinal)
            ? asked
            : model.DefaultEffort ?? model.EffortLevels[0];
    }

    private static bool Listed(ModelInfo model, IReadOnlySet<string> installed) => model.Kind switch
    {
        ModelInfo.KindText => !model.Local,
        ModelInfo.KindImage or ModelInfo.KindAudio => true,

        // Bileşene bağlı motor (Remotion) yalnızca ajan kurulu bildirdiyse listelenir (Req 2, 19).
        ModelInfo.KindVideoEngine => model.Component is null || installed.Contains(model.Component),
        _ => false,
    };

    private static string StatusName(ProductionJobStatus status) => status switch
    {
        ProductionJobStatus.Queued => "queued",
        ProductionJobStatus.Producing => "producing",
        ProductionJobStatus.Stopped => "stopped",
        ProductionJobStatus.WaitingForAgent => "waiting_for_agent",
        ProductionJobStatus.Failed => "failed",
        _ => "completed",
    };

    private static ProductionSettingsDto ToDto(ProductionSettings s) =>
        new(s.Subtitles, s.ProgressBar, s.CodeModel, s.CodeEffort, s.Engine, s.ImageModel, s.AudioModel);

    private static FixedSettingsDto? ToDto(FixedJobSettings? f) =>
        f is null
            ? null
            : new FixedSettingsDto(
                new ProductionSettingsDto(f.Subtitles, f.ProgressBar, f.CodeModel, f.CodeEffort, f.Engine, f.ImageModel, f.AudioModel),
                f.Language,
                f.Format,
                f.TargetDurationSeconds);

    /// <summary>İşi başlatır: ayarları ve konseptin kaydındaki değerleri sabitler (devam eden iş ilk sabitlenen değerleri korur).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="job">İş.</param>
    /// <param name="settings">Profilin güncel ayarları.</param>
    /// <param name="from">Beklenen durumlar.</param>
    /// <returns>Hata ya da <c>null</c>.</returns>
    private async Task<Error?> StartJobAsync(Guid userId, ProductionJob job, ProductionSettings settings, IReadOnlyCollection<ProductionJobStatus> from)
    {
        var now = time.GetUtcNow();
        var fix = JobFixing.Fix(job, settings, now);
        if (!await jobs.StartAsync(userId, job.Id, from, fix, now, CancellationToken.None).ConfigureAwait(false))
        {
            return ProductionErrors.StartBlocked(StartBlock.RunningElsewhere); // Bu arada başka iş başladı (tekil çalışan kısıtı).
        }

        await concepts.ChangeAsync(userId, job.ConceptId, ConceptQueueStage.Queued, ConceptQueueStage.Producing, CancellationToken.None).ConfigureAwait(false);
        return null;
    }

    /// <summary>Sıra uyarısını kaldırır ve koşucuyu isteği yapanın kimliğiyle başlatır (spec 0017 Req 5, 11).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Her zaman <c>null</c> (hata yok).</returns>
    private async Task<Error?> LaunchAsync(Guid userId)
    {
        await pipeline.Queue.SetAsync(new QueueState { UserId = userId, UpdatedAt = time.GetUtcNow() }, CancellationToken.None).ConfigureAwait(false);
        if (pipeline.Caller.Current() is { } caller)
        {
            pipeline.Runner.Launch(userId, caller);
        }

        return null;
    }

    private async Task<IReadOnlyList<ModelInfo>?> ListedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var all = await models.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var installed = components.Get(userId);
        return all?.Where(m => Listed(m, installed)).ToList();
    }

    private async Task<ProductionSettings> CurrentSettingsAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        await settingsStore.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false)
        ?? ProductionDefaults.For(userId, profileId, components.Get(userId));

    private async Task<ProductionStateDto> BuildStateAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        var settings = await CurrentSettingsAsync(userId, profileId, cancellationToken).ConfigureAwait(false);
        var listed = await ListedAsync(userId, cancellationToken).ConfigureAwait(false);
        var all = await jobs.ListActiveAsync(userId, cancellationToken).ConfigureAwait(false);
        var mine = all.Where(j => j.ProfileId == profileId).OrderBy(j => j.Position).ToList();
        var runningHere = mine.Any(j => Running.Contains(j.Status));
        var runningElsewhere = all.Any(j => j.ProfileId != profileId && Running.Contains(j.Status));
        var languages = mine.Where(j => j.Status == ProductionJobStatus.Queued).Select(j => j.Language).ToList();
        var selectable = listed?.Select(m => new SelectableModel(m.Id, m.Kind, m.Availability, m.LanguageOnly)).ToList();
        var block = StartBlock.Evaluate(settings, selectable, languages, runningHere, runningElsewhere);
        return new ProductionStateDto(
            profileId,
            ToDto(settings),
            [.. (listed ?? []).Select(m => new ProductionModelDto(
                m.Id, m.Name, m.Kind, m.Local, m.LanguageOnly, m.LicenseWarning, m.EffortLevels, m.DefaultEffort, m.Availability))],
            listed is null,
            [.. mine.Select((j, i) => new ProductionJobDto(
                j.Id,
                j.ConceptId,
                j.Title,
                i + 1,
                StatusName(j.Status),
                j.Language,
                ToDto(j.FixedSettings),
                [.. (j.Progress?.Stages ?? []).Select(ToDto)],
                j.Progress?.CostUsd ?? 0m,
                j.Status == ProductionJobStatus.Failed ? j.Progress?.Error : null))],
            block,
            block is null ? null : ProductionErrors.BlockText(block),
            [.. StartBlock.LanguageWarnings(settings, selectable, languages).Select(ProductionErrors.BlockText)],
            runningHere,
            pipeline.Agents.ConnectedDevice(userId) is not null,
            (await pipeline.Queue.GetAsync(userId, cancellationToken).ConfigureAwait(false))?.Halted == true,
            pipeline.Agents.UpdateRequired(userId));
    }

    private async Task<Result<ProductionStateDto>> WithProfileAsync(
        Guid profileId, Func<Guid, CancellationToken, Task<Error?>> action, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId)
        {
            return Result<ProductionStateDto>.Failure(ProductionErrors.SignInRequired);
        }

        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<ProductionStateDto>.Failure(ProductionErrors.ProfileNotFound);
        }

        if (await action(userId, cancellationToken).ConfigureAwait(false) is { } error)
        {
            return Result<ProductionStateDto>.Failure(error);
        }

        return Result<ProductionStateDto>.Success(await BuildStateAsync(userId, profileId, cancellationToken).ConfigureAwait(false));
    }

    private async Task<Result<ProductionStateDto>> WithJobAsync(
        Guid jobId, Func<Guid, ProductionJob, CancellationToken, Task<Error?>> action, CancellationToken cancellationToken)
    {
        if (user.UserId is not { } userId)
        {
            return Result<ProductionStateDto>.Failure(ProductionErrors.SignInRequired);
        }

        var job = await jobs.GetAsync(userId, jobId, cancellationToken).ConfigureAwait(false);
        if (job is null || job.Status == ProductionJobStatus.Completed)
        {
            return Result<ProductionStateDto>.Failure(ProductionErrors.JobNotFound);
        }

        if (await action(userId, job, cancellationToken).ConfigureAwait(false) is { } error)
        {
            return Result<ProductionStateDto>.Failure(error);
        }

        return Result<ProductionStateDto>.Success(await BuildStateAsync(userId, job.ProfileId, cancellationToken).ConfigureAwait(false));
    }
}
