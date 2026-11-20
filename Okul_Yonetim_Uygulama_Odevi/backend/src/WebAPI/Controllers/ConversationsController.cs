using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication.Messages;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Öğretmen–veli mesaj uçları (spec 0011; okul seçilmiş olmalı): konuşma listesi, "yeni mesaj" seçenekleri, konuşma başlatma
/// (var olan döner), mesajlar (son 50, <c>before</c> ile öncekiler; açılınca okundu) ve gönderme (yanıtta mesai dışı bilgisi).
/// Konuşmayı yalnızca iki taraf görür (başkası 404); öğretmen/veli rolü olmayan başlatamaz (403); ilişki bitmişse gönderme 400.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/conversations")]
public sealed class ConversationsController : ControllerBase
{
    private readonly ISender sender;

    public ConversationsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ConversationRow>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationRow>>> List(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListConversationsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpGet("contacts")]
    [ProducesResponseType<MessageContacts>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MessageContacts>> Contacts(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListMessageContactsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<ConversationRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationRow>> Start(StartConversationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new StartConversationCommand(request.StudentId, request.CounterpartUserId), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType<ConversationThread>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationThread>> Messages(Guid id, [FromQuery] Guid? before, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetConversationMessagesQuery(id, before), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType<MessageSent>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageSent>> Send(Guid id, SendMessageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new SendMessageCommand(id, request.Body, request.AiDraftId), cancellationToken).ConfigureAwait(false));
    }
}
