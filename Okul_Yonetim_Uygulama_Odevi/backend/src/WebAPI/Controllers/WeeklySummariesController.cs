using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Ai.WeeklySummaries;
using OkulYonetim.Application.Features.Communication.WeeklySummaries;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Haftalık gelişim özeti uçları (spec 0014; okul seçilmiş olmalı, yalnızca öğrencinin sınıfına atanmış öğretmen — rol ve ilişki
/// işleyicide: rol yoksa 403, sınıf/öğrenci erişimi yoksa 404): sınıflar, sınıf + hafta öğrenci listesi, öğrenci paneli (dayanak +
/// taslak), "Özet oluştur" (veliye hiçbir şey gitmez), "Onayla ve gönder" (Communication'ın komutu — veliye giden tek çıkış) ve
/// "Vazgeç". Hafta <c>week=current|previous</c> (boşsa bu hafta).
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/weekly-summaries")]
public sealed class WeeklySummariesController : ControllerBase
{
    private readonly ISender sender;

    public WeeklySummariesController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet("classrooms")]
    [ProducesResponseType<IReadOnlyList<WeeklySummaryClassroom>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<WeeklySummaryClassroom>>> Classrooms(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListWeeklySummaryClassroomsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpGet]
    [ProducesResponseType<WeeklySummaryList>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeeklySummaryList>> List([FromQuery] Guid classroomId, [FromQuery] string? week, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListWeeklySummaryStatusQuery(classroomId, week), cancellationToken).ConfigureAwait(false));

    [HttpGet("students/{studentId:guid}")]
    [ProducesResponseType<WeeklySummaryPanel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeeklySummaryPanel>> Panel(Guid studentId, [FromQuery] string? week, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetWeeklySummaryPanelQuery(studentId, week), cancellationToken).ConfigureAwait(false));

    [HttpPost("students/{studentId:guid}/draft")]
    [ProducesResponseType<WeeklySummaryPanel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeeklySummaryPanel>> Generate(Guid studentId, [FromQuery] string? week, GenerateWeeklySummaryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new GenerateWeeklySummaryDraftCommand(studentId, week, request.TeacherNote), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("students/{studentId:guid}/send")]
    [ProducesResponseType<WeeklySummarySent>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeeklySummarySent>> Send(Guid studentId, [FromQuery] string? week, SendWeeklySummaryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new SendWeeklySummaryCommand(studentId, week, request.Text, request.DraftId, request.DraftGeneratedAt), cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("students/{studentId:guid}/draft")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Discard(Guid studentId, [FromQuery] string? week, CancellationToken cancellationToken)
    {
        await this.sender.Send(new DiscardWeeklySummaryDraftCommand(studentId, week), cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }
}
