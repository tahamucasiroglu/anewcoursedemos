using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Concrete;
using TahaKonsept.Modules.Production.Application.Mapping;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IPronunciationService"/> ortak davranışı (oturumdaki kullanıcı): sözlük kayıtlarını yönetir (aynı yazılış tek kayıt; öneriyi onaylamak
/// = <c>IsSuggestion=false</c> ile kaydetmek) ve "Dinle" önizlemesi yapar: metin onaylı sözlükle normalize edilir, kullanıcının ses ayarına göre
/// yerelde ajan (<see cref="IAgentGateway.PreviewSpeechAsync"/> → ajan sunucusundaki ses adresi) ya da online sağlayıcı (base64 <c>data:</c> adresi) seslendirir.
/// </summary>
public abstract class PronunciationServiceBase : IPronunciationService
{
    private readonly IPronunciationRepository _repository;
    private readonly IProductionSettingsService _settings;
    private readonly ISpeechTextNormalizer _normalizer;
    private readonly IAgentGateway _agent;
    private readonly IMediaGateway _media;
    private readonly ICurrentUser _currentUser;

    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Sözlük deposu.</param>
    /// <param name="settings">Ayar servisi.</param>
    /// <param name="normalizer">Okunuş düzeltici.</param>
    /// <param name="agent">Ajan geçidi.</param>
    /// <param name="media">Online ses geçidi.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    protected PronunciationServiceBase(
        IPronunciationRepository repository,
        IProductionSettingsService settings,
        ISpeechTextNormalizer normalizer,
        IAgentGateway agent,
        IMediaGateway media,
        ICurrentUser currentUser)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _media = media ?? throw new ArgumentNullException(nameof(media));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Önizleme bekleme süresi.</summary>
    protected virtual TimeSpan PreviewTimeout => TimeSpan.FromSeconds(60);

    /// <summary>Kayıtlar (onaylılar önce).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    public async Task<Result<IReadOnlyList<PronunciationDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var entries = await _repository.ListForUserAsync(_currentUser.GetRequiredUserId(), cancellationToken);
        return Result.Success<IReadOnlyList<PronunciationDto>>(entries
            .OrderBy(e => e.Status == PronunciationStatus.Approved ? 0 : 1)
            .ThenBy(e => e.WrittenKey, StringComparer.Ordinal)
            .Select(e => e.ToDto())
            .ToList());
    }

    /// <summary>Ekler/günceller.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt.</returns>
    public async Task<Result<PronunciationDto>> SaveAsync(SavePronunciationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = _currentUser.GetRequiredUserId();
        var status = request.IsSuggestion ? PronunciationStatus.Suggested : PronunciationStatus.Approved;
        Pronunciation? entry;
        if (request.Id is Guid id)
        {
            entry = await _repository.GetForUserAsync(id, userId, cancellationToken);
            if (entry is null)
            {
                return ProductionErrors.PronunciationNotFound;
            }
        }
        else
        {
            entry = await _repository.FindByKeyAsync(userId, Pronunciation.KeyOf(request.Written), cancellationToken);
        }

        if (entry is null)
        {
            entry = Pronunciation.Create(userId, request.Written, request.Spoken, status);
            await _repository.AddAsync(entry, cancellationToken);
        }
        else
        {
            entry.Update(request.Written, request.Spoken, status);
        }

        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return entry.ToDto();
    }

    /// <summary>Siler.</summary>
    /// <param name="id">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetForUserAsync(id, _currentUser.GetRequiredUserId(), cancellationToken);
        if (entry is null)
        {
            return Result.Failure(ProductionErrors.PronunciationNotFound);
        }

        _repository.Remove(entry);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Önizleme.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result<PronunciationPreviewResponse>> PreviewAsync(PronunciationPreviewRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = _currentUser.GetRequiredUserId();
        var settings = await _settings.GetForUserAsync(userId, cancellationToken);
        var text = request.Text.Trim();
        var spoken = text;
        if (request.Normalize)
        {
            var lexicon = (await _repository.ListApprovedAsync(userId, cancellationToken))
                .Select(p => new PronunciationEntry { Written = p.Written, Spoken = p.Spoken })
                .ToList();
            spoken = _normalizer.Normalize(text, "tr", lexicon).Spoken;
        }

        var response = new PronunciationPreviewResponse { DisplayText = text, SpokenText = spoken };
        if (settings.SpeechMode == MediaMode.Off || string.IsNullOrWhiteSpace(settings.SpeechModelKey))
        {
            return response with { Message = "Ses kapalı; Konseptten Video ayarlarından bir ses modeli seçin." };
        }

        return settings.SpeechMode == MediaMode.Local
            ? await PreviewLocalAsync(userId, settings, response, cancellationToken)
            : await PreviewOnlineAsync(userId, settings, response, cancellationToken);
    }

    private async Task<Result<PronunciationPreviewResponse>> PreviewLocalAsync(Guid userId, ProductionSettingsDto settings, PronunciationPreviewResponse response, CancellationToken cancellationToken)
    {
        var preview = await _agent.PreviewSpeechAsync(
            userId,
            new SpeechPreviewCommand
            {
                RequestId = Guid.CreateVersion7(),
                EngineId = settings.SpeechModelKey!,
                VoiceId = settings.VoiceId,
                Text = response.SpokenText,
                Settings = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["speed"] = settings.SpeechSpeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["language"] = "tr",
                },
            },
            PreviewTimeout,
            cancellationToken);
        if (preview.IsFailure)
        {
            return response with { Source = "agent", Message = preview.Error.Message };
        }

        if (!preview.Value.Success || string.IsNullOrWhiteSpace(preview.Value.AudioPath))
        {
            return response with { Source = "agent", Message = preview.Value.ErrorMessage ?? "Önizleme sesi üretilemedi." };
        }

        var url = _agent.GetAgentFileUrl(userId, preview.Value.AudioPath);
        return url.IsSuccess
            ? response with { Source = "agent", AudioUrl = url.Value, MimeType = "audio/wav" }
            : response with { Source = "agent", Message = url.Error.Message };
    }

    private async Task<Result<PronunciationPreviewResponse>> PreviewOnlineAsync(Guid userId, ProductionSettingsDto settings, PronunciationPreviewResponse response, CancellationToken cancellationToken)
    {
        var spoken = await _media.SynthesizeSpeechAsync(
            settings.SpeechModelKey!,
            new SpeechRequest
            {
                Context = new CallContext(userId, "speech-preview"),
                Text = response.SpokenText,
                Voice = settings.VoiceId,
                Language = SpeechStageHandler.ToLocale("tr"),
                Speed = (decimal)settings.SpeechSpeed,
                Format = "mp3",
            },
            cancellationToken);
        if (spoken.IsFailure)
        {
            return response with { Source = "online", Message = spoken.Error.Message };
        }

        var base64 = Convert.ToBase64String(spoken.Value.Audio);
        var mime = string.IsNullOrWhiteSpace(spoken.Value.MimeType) ? "audio/mpeg" : spoken.Value.MimeType;
        return response with { Source = "online", AudioBase64 = base64, MimeType = mime, AudioUrl = "data:" + mime + ";base64," + base64 };
    }
}
