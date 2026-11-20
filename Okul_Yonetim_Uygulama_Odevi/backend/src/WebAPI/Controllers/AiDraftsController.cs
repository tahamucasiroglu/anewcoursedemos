using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Ai.Drafts;
using OkulYonetim.Application.Features.Ai.VoiceMessages;
using OkulYonetim.WebAPI.Auth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// AI taslak uçları (spec 0012; okul seçilmiş olmalı): sesli nottan veli mesajı taslağı (multipart: <c>conversationId</c> +
/// <c>audio</c>; yanıtta taslak kimliği, döküm ve taslak — veliye hiçbir şey gitmez) ve "Vazgeç" (taslak silinir). Gönderim
/// taslak kimliğiyle konuşmanın mesaj ucundan yapılır. Ses bellekte tutulur: istek gövdesi sınırı ~2,1 MB ve form tamponu
/// bellek eşiği bu sınırın üstünde — ASP.NET sesi geçici diske yazmaz (gereksinim 7).
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/ai")]
public sealed class AiDraftsController : ControllerBase
{
    /// <summary>2 MB ses + çok parçalı form başlıkları için pay.</summary>
    public const int MaxRequestBytes = CreateVoiceMessageDraftCommand.MaxAudioBytes + (100 * 1024);

    private readonly ISender sender;

    public AiDraftsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpPost("voice-message-drafts")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes, MemoryBufferThreshold = MaxRequestBytes + 1)]
    [ProducesResponseType<VoiceMessageDraftDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceMessageDraftDto>> CreateVoiceMessageDraft([FromForm] Guid? conversationId, IFormFile? audio, CancellationToken cancellationToken)
    {
        // Dosya istek sınırıyla (~2,1 MB) zaten sınırlı; boyut ve tür kuralı doğrulayıcıda.
        byte[] bytes = [];
        if (audio is not null && audio.Length > 0)
        {
            using MemoryStream buffer = new((int)audio.Length);
            await audio.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            bytes = buffer.ToArray();
        }

        return this.Ok(await this.sender.Send(new CreateVoiceMessageDraftCommand(conversationId, bytes, audio?.ContentType), cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("drafts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DiscardDraft(Guid id, CancellationToken cancellationToken)
    {
        await this.sender.Send(new DiscardAiDraftCommand(id), cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }
}
