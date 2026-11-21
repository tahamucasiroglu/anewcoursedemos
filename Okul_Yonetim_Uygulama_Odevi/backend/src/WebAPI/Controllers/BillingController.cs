using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Billing;
using OkulYonetim.Application.Features.Billing.Discounts;
using OkulYonetim.Application.Features.Billing.Ledger;
using OkulYonetim.Application.Features.Billing.Payments;
using OkulYonetim.Application.Features.Billing.Plans;
using OkulYonetim.Application.Features.Billing.Tariffs;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Tahsilat ve ön muhasebe uçları (spec 0015; okul seçilmiş olmalı): tarife ve indirim (okuma kurucu + muhasebe, yazma yalnızca
/// kurucu), planı olan öğrencilerin cari listesi ve öğrenci carisi, plan önizleme/açma/iptal, tahsilat/iptal ve makbuz. Rol ve şube
/// her istekte uygulamada (<c>BillingGuard</c>): öğretmen ve veli 403; başka okulun ya da muhasebenin şubesi dışındaki kayıt 404.
/// Alan hataları ve iş kuralı ihlalleri 400 (Türkçe).
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly ISender sender;

    public BillingController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet("tariffs")]
    [ProducesResponseType<IReadOnlyList<FeeScheduleRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<FeeScheduleRow>>> Tariffs(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListFeeSchedulesQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost("tariffs")]
    [ProducesResponseType<FeeScheduleRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FeeScheduleRow>> CreateTariff(FeeScheduleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateFeeScheduleCommand(request.Name, JsonValueText.Of(request.Amount)), cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("tariffs/{id:guid}")]
    [ProducesResponseType<FeeScheduleRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeeScheduleRow>> UpdateTariff(Guid id, FeeScheduleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new UpdateFeeScheduleCommand(id, request.Name, JsonValueText.Of(request.Amount)), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("tariffs/{id:guid}/deactivate")]
    [ProducesResponseType<FeeScheduleRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeeScheduleRow>> DeactivateTariff(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetFeeScheduleActiveCommand(id, false), cancellationToken).ConfigureAwait(false));

    [HttpPost("tariffs/{id:guid}/activate")]
    [ProducesResponseType<FeeScheduleRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeeScheduleRow>> ActivateTariff(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetFeeScheduleActiveCommand(id, true), cancellationToken).ConfigureAwait(false));

    [HttpGet("discounts")]
    [ProducesResponseType<IReadOnlyList<DiscountRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<DiscountRow>>> Discounts(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListDiscountsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost("discounts")]
    [ProducesResponseType<DiscountRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DiscountRow>> CreateDiscount(DiscountRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateDiscountCommand(request.Name, JsonValueText.Of(request.Percent)), cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("discounts/{id:guid}")]
    [ProducesResponseType<DiscountRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscountRow>> UpdateDiscount(Guid id, DiscountRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new UpdateDiscountCommand(id, request.Name, JsonValueText.Of(request.Percent)), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("discounts/{id:guid}/deactivate")]
    [ProducesResponseType<DiscountRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscountRow>> DeactivateDiscount(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetDiscountActiveCommand(id, false), cancellationToken).ConfigureAwait(false));

    [HttpPost("discounts/{id:guid}/activate")]
    [ProducesResponseType<DiscountRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscountRow>> ActivateDiscount(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetDiscountActiveCommand(id, true), cancellationToken).ConfigureAwait(false));

    [HttpGet("students")]
    [ProducesResponseType<StudentAccountPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StudentAccountPage>> Students([FromQuery] Guid? branchId, [FromQuery] bool overdueOnly, [FromQuery] string? search, [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        this.Ok(await this.sender.Send(new ListStudentAccountsQuery(branchId, overdueOnly, search, page), cancellationToken).ConfigureAwait(false));

    [HttpGet("students/{id:guid}")]
    [ProducesResponseType<StudentAccount>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentAccount>> Student(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetStudentAccountQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpPost("plans/preview")]
    [ProducesResponseType<PlanPreview>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanPreview>> PreviewPlan(InstallmentPlanRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new PreviewInstallmentPlanQuery(request.StudentId, request.FeeScheduleId, request.DiscountId, request.InstallmentCount, request.FirstDueDate), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("plans")]
    [ProducesResponseType<BillingSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BillingSaved>> CreatePlan(InstallmentPlanRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateInstallmentPlanCommand(request.StudentId, request.FeeScheduleId, request.DiscountId, request.InstallmentCount, request.FirstDueDate), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("plans/{id:guid}/cancel")]
    [ProducesResponseType<BillingSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BillingSaved>> CancelPlan(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new CancelInstallmentPlanCommand(id), cancellationToken).ConfigureAwait(false));

    [HttpPost("payments")]
    [ProducesResponseType<PaymentRecorded>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentRecorded>> RecordPayment(PaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new RecordPaymentCommand(request.PlanId, JsonValueText.Of(request.Amount), request.Date, request.Method, request.Note), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("payments/{id:guid}/cancel")]
    [ProducesResponseType<BillingSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BillingSaved>> CancelPayment(Guid id, CancelPaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CancelPaymentCommand(id, request.Reason), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("payments/{id:guid}/receipt")]
    [ProducesResponseType<Receipt>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Receipt>> Receipt(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetReceiptQuery(id), cancellationToken).ConfigureAwait(false));
}
