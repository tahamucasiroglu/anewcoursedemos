using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Application.Resources;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application.Base;

/// <summary>
/// Deneme çağrısı (plan 0013 Risk 3): isteği yapan kullanıcı ve sunucuda kurulmuş yetkileriyle kapıyı çağırır; istem sabit ve
/// kısadır, çıktı sınırı <see cref="TrialOutputTokens"/> (katalog limitinden küçükse), efor verilmezse en düşük efor. Yalnızca metin modelleri denenir (görsel
/// maliyetli olduğundan deneme ucundan çağrılmaz). Yanıt metni <see cref="MaxTextLength"/> karaktere kısaltılır.
/// </summary>
/// <param name="gateway">Çağrı kapısı.</param>
/// <param name="catalog">Katalog.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="permissions">Yetkiler.</param>
public abstract class TrialCallServiceBase(IAiCallGateway gateway, ICatalogSource catalog, ICurrentUser currentUser, ICallerPermissions permissions)
    : ITrialCallService
{
    /// <summary>Deneme çağrısının çıktı sınırı (token).</summary>
    public const int TrialOutputTokens = 4096;

    /// <summary>Yanıtta dönen metnin en fazla uzunluğu.</summary>
    public const int MaxTextLength = 500;

    /// <inheritdoc />
    public async Task<Result<TrialCallDto>> CallAsync(TrialCallRequest? request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<TrialCallDto>.Failure(AiGatewayErrors.SignInRequired);
        }

        var model = catalog.Catalog.Models.FirstOrDefault(m => string.Equals(m.Id, request?.ModelId, StringComparison.Ordinal));
        if (model is null)
        {
            return Result<TrialCallDto>.Failure(AiGatewayErrors.ModelNotFound);
        }

        if (model.Kind != ModelKind.Text)
        {
            return Result<TrialCallDto>.Failure(AiGatewayErrors.ModelNotCallable);
        }

        var result = await gateway.CallAsync(
            new AiCallRequest
            {
                Caller = new AiCaller(userId, permissions.Current),
                ModelId = model.Id,
                Purpose = CallPurpose.Trial,
                Effort = string.IsNullOrWhiteSpace(request!.Effort) ? LowestEffort(model.EffortLevels) : request.Effort,
                MaxOutputTokens = TrialOutputTokens,
                Prompt = AiGatewayText.TrialPrompt,
            },
            cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return Result<TrialCallDto>.Failure(result.Error!);
        }

        var value = result.Value;
        var text = value.Text ?? string.Empty;
        return Result<TrialCallDto>.Success(new TrialCallDto(
            value.ModelId,
            text.Length > MaxTextLength ? text[..MaxTextLength] + "…" : text,
            value.Outcome == CallOutcome.Truncated,
            value.InputTokens,
            value.OutputTokens,
            value.CostUsd,
            value.IsEstimated,
            value.Attempts));
    }

    // Deneme hızlı bitsin (Web → Api isteği 15 sn): efor verilmezse modelin desteklediği en düşük efor.
    private static string? LowestEffort(IReadOnlyList<string> levels) =>
        levels.Contains("low", StringComparer.Ordinal) ? "low" : null;
}
