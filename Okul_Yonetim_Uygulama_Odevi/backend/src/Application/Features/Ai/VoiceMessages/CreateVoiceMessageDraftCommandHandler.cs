using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Ai.Prompts;
using OkulYonetim.Application.Features.Communication.Messages;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Ai.VoiceMessages;

/// <summary>
/// Sesli not → veli mesajı taslağı (spec 0012 gereksinim 1–7). Sıra: konuşma bağlamı Communication'ın sorgusundan (taraf değil
/// → 404, öğretmen tarafı değil → 403, ilişki bitti → "Bu konuşmaya artık mesaj gönderilemez.") → ses servisi (döküm + süre)
/// → süre 60 sn'yi aşarsa ya da döküm boşsa red (metin servisi çağrılmaz) → metin servisi (yalnızca sabit talimat + döküm —
/// veri minimizasyonu) → taslak 1000'e kısaltılır → <see cref="AiDraft"/> "bekliyor" kaydedilir. Veliye hiçbir şey gitmez
/// (BR-5); Ai, Communication'a yazmaz. Servis aksaklığı Warning loglanır ve "Yapay zekâ şu an kullanılamıyor…" döner.
/// Ses yalnızca bellekte; döküm ve taslak loga yazılmaz — logda kimlikler, süre, karakter sayıları ve ölçülen süreler.
/// </summary>
public sealed class CreateVoiceMessageDraftCommandHandler : IRequestHandler<CreateVoiceMessageDraftCommand, VoiceMessageDraftDto>
{
    private readonly ISender sender;
    private readonly ICurrentSession session;
    private readonly ISpeechToText speech;
    private readonly IAiTextGenerator generator;
    private readonly IAiDraftRepository drafts;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<CreateVoiceMessageDraftCommandHandler> logger;

    public CreateVoiceMessageDraftCommandHandler(
        ISender sender,
        ICurrentSession session,
        ISpeechToText speech,
        IAiTextGenerator generator,
        IAiDraftRepository drafts,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<CreateVoiceMessageDraftCommandHandler> logger)
    {
        this.sender = sender;
        this.session = session;
        this.speech = speech;
        this.generator = generator;
        this.drafts = drafts;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<VoiceMessageDraftDto> Handle(CreateVoiceMessageDraftCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Guid conversationId = request.ConversationId!.Value;
        TeacherConversationContext context = await this.sender.Send(new GetTeacherConversationContextQuery(conversationId), cancellationToken).ConfigureAwait(false);
        if (!context.IsTeacher)
        {
            throw new ForbiddenException(await this.localization.AiTextAsync(AiMessages.Forbidden).ConfigureAwait(false));
        }

        if (!context.CanSend)
        {
            throw new BusinessException(await this.localization.AiTextAsync(AiMessages.ConversationClosed).ConfigureAwait(false));
        }

        Guid tenantId = this.session.TenantId ?? throw new InvalidOperationException("Sesli not için seçili okul gerekli.");
        Guid userId = this.session.UserId ?? throw new InvalidOperationException("Sesli not için oturum gerekli.");

        Stopwatch watch = Stopwatch.StartNew();
        SpeechToTextResult heard;
        using (MemoryStream audio = new(request.Audio.ToArray(), writable: false))
        {
            heard = await this.CallAsync(() => this.speech.TranscribeAsync(audio, request.ContentType!, cancellationToken), "ses", conversationId, userId).ConfigureAwait(false);
        }

        long speechMs = watch.ElapsedMilliseconds;
        if (heard.DurationSeconds > CreateVoiceMessageDraftCommand.MaxDurationSeconds)
        {
            this.logger.LogWarning("Sesli not reddedildi (uzun): konuşma {ConversationId}, öğretmen {UserId}, ses {DurationSeconds} sn", conversationId, userId, heard.DurationSeconds);
            throw new BusinessException(await this.localization.AiTextAsync(AiMessages.TooLong).ConfigureAwait(false));
        }

        if (string.IsNullOrWhiteSpace(heard.Text))
        {
            this.logger.LogWarning("Sesli not reddedildi (boş döküm): konuşma {ConversationId}, öğretmen {UserId}, ses {DurationSeconds} sn", conversationId, userId, heard.DurationSeconds);
            throw new BusinessException(await this.localization.AiTextAsync(AiMessages.NotUnderstood).ConfigureAwait(false));
        }

        watch.Restart();
        AiTextResult written = await this.CallAsync(
            () => this.generator.GenerateAsync(new AiTextRequest(AiPrompts.VoiceMessage, heard.Text.Trim(), AiPrompts.VoiceMessageMaxTokens), cancellationToken),
            "metin",
            conversationId,
            userId).ConfigureAwait(false);
        long textMs = watch.ElapsedMilliseconds;
        if (string.IsNullOrWhiteSpace(written.Text))
        {
            this.logger.LogWarning("AI kullanılamıyor: adım {Step}, neden {Reason}, konuşma {ConversationId}, öğretmen {UserId}", "metin", "empty", conversationId, userId);
            throw new BusinessException(await this.localization.AiTextAsync(AiMessages.Unavailable).ConfigureAwait(false));
        }

        AiDraft draft = AiDraft.VoiceMessage(tenantId, userId, context.StudentId, conversationId, heard.Text, written.Text, this.timeProvider.GetUtcNow());
        this.drafts.Add(draft);
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation(
            "Sesli not taslağı oluşturuldu: taslak {DraftId}, konuşma {ConversationId}, öğretmen {UserId}, öğrenci {StudentId}, ses {DurationSeconds} sn, döküm {TranscriptLength} karakter, taslak {DraftLength} karakter, ses→metin {SpeechMs} ms, metin {TextMs} ms",
            draft.Id,
            conversationId,
            userId,
            context.StudentId,
            heard.DurationSeconds,
            draft.SourceText.Length,
            draft.DraftText.Length,
            speechMs,
            textMs);
        return new VoiceMessageDraftDto(draft.Id, draft.SourceText, draft.DraftText, heard.DurationSeconds);
    }

    private async Task<T> CallAsync<T>(Func<Task<T>> call, string step, Guid conversationId, Guid userId)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (AiUnavailableException exception)
        {
            this.logger.LogWarning("AI kullanılamıyor: adım {Step}, neden {Reason}, konuşma {ConversationId}, öğretmen {UserId}", step, exception.Reason, conversationId, userId);
            throw new BusinessException(await this.localization.AiTextAsync(AiMessages.Unavailable).ConfigureAwait(false));
        }
    }
}
