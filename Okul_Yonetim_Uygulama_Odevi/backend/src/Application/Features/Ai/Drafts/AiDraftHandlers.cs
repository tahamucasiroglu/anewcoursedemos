using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Ai.Drafts;

/// <summary>
/// AI taslağının yaşam döngüsü (spec 0012 C-4): bekleyen taslağı onay için bulma (sahip + tür + bağlam; kullanılmışsa iş
/// hatası), onaylandı işaretleme (kaydı çağıran yapar) ve "Vazgeç" (silme — taslaklı gönderimle aynı taslak sırası
/// <c>ai-draft:&lt;id&gt;</c> içinde: eşzamanlı gönder/vazgeç'te ya mesaj + onaylı taslak kalır ya da ikisi de yok; sıra
/// dolarsa "Taslak şu an işleniyor…"). Taslak kurum filtreli okunur; başka kişinin,
/// başka okulun ya da başka bağlamın taslağı "bulunamadı" (404 — "var ama sizin değil" sızdırılmaz). Loglarda yalnızca kimlikler.
/// </summary>
public sealed class AiDraftHandlers :
    IRequestHandler<GetPendingAiDraftQuery, Guid>,
    IRequestHandler<MarkAiDraftApprovedCommand, bool>,
    IRequestHandler<DiscardAiDraftCommand, bool>
{
    private readonly IAiDraftRepository drafts;
    private readonly ICurrentSession session;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<AiDraftHandlers> logger;

    public AiDraftHandlers(
        IAiDraftRepository drafts,
        ICurrentSession session,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<AiDraftHandlers> logger)
    {
        this.drafts = drafts;
        this.session = session;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Guid> Handle(GetPendingAiDraftQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AiDraft draft = await this.OwnAsync(request.DraftId, cancellationToken).ConfigureAwait(false);
        if (draft.Kind != request.Kind || draft.ContextId != request.ContextId)
        {
            throw await this.NotFoundAsync().ConfigureAwait(false);
        }

        return draft.IsPending
            ? draft.Id
            : throw new BusinessException(await this.localization.AiTextAsync(AiMessages.DraftUsed).ConfigureAwait(false));
    }

    public async Task<bool> Handle(MarkAiDraftApprovedCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AiDraft draft = await this.OwnAsync(request.DraftId, cancellationToken).ConfigureAwait(false);
        if (!draft.IsPending)
        {
            throw new BusinessException(await this.localization.AiTextAsync(AiMessages.DraftUsed).ConfigureAwait(false));
        }

        draft.Approve(request.FinalText, draft.CreatedByUserId, request.ResultId, this.timeProvider.GetUtcNow());
        this.logger.LogInformation(
            "AI taslağı onaylandı: taslak {DraftId}, onaylayan {UserId}, sonuç {ResultId}, son metin {FinalLength} karakter, düzenlendi {Edited}",
            draft.Id,
            draft.CreatedByUserId,
            request.ResultId,
            request.FinalText.Length,
            !string.Equals(draft.DraftText, request.FinalText, StringComparison.Ordinal));
        return true;
    }

    public async Task<bool> Handle(DiscardAiDraftCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync(AiDraftLock.Key(request.Id), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            this.logger.LogWarning("AI taslağı sırası dolu (vazgeç): taslak {DraftId}", request.Id);
            throw new BusinessException(await this.localization.AiTextAsync(AiMessages.DraftBusy).ConfigureAwait(false));
        }

        await using (scope.ConfigureAwait(false))
        {
            AiDraft draft = await this.OwnAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (!draft.IsPending)
            {
                throw await this.NotFoundAsync().ConfigureAwait(false);
            }

            this.drafts.Remove(draft);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("AI taslağından vazgeçildi (silindi): taslak {DraftId}, kişi {UserId}", draft.Id, draft.CreatedByUserId);
            return true;
        }
    }

    /// <summary>Oturumdaki kişinin oluşturduğu taslak (izlenen); yoksa ya da başkasınınsa 404.</summary>
    private async Task<AiDraft> OwnAsync(Guid id, CancellationToken cancellationToken)
    {
        AiDraft? draft = await this.drafts.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return draft is not null && this.session.UserId is { } userId && draft.CreatedByUserId == userId
            ? draft
            : throw await this.NotFoundAsync().ConfigureAwait(false);
    }

    private async Task<NotFoundException> NotFoundAsync() =>
        new(await this.localization.AiTextAsync(AiMessages.DraftNotFound).ConfigureAwait(false));
}
