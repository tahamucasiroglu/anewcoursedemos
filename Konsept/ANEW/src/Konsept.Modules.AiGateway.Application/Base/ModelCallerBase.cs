using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.AiGateway.Contracts.Enums;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application.Base;

/// <summary>
/// AiGateway.Contracts <see cref="IModelCaller"/> uygulaması (spec 0014): rolü harcama amacına çevirip <see cref="IAiCallGateway"/>'e
/// verir (rol: üretim hattının analiz/kod/düzeltme rolleri → <see cref="CallPurpose.VideoCode"/>, konu önerisi → <see cref="CallPurpose.TopicSuggestion"/>, konsept → <see cref="CallPurpose.ConceptGeneration"/>, anlamca
/// tekrar denetimi → <see cref="CallPurpose.Review"/>). Çıktı sınırı verilmez; kapı katalog limitini uygular.
/// </summary>
/// <param name="gateway">Çağrı kapısı.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="permissions">Sunucuda kurulmuş yetkiler.</param>
public abstract class ModelCallerBase(IAiCallGateway gateway, ICurrentUser currentUser, ICallerPermissions permissions) : IModelCaller
{
    /// <inheritdoc />
    public ModelCallerIdentity? Current() =>
        currentUser.UserId is { } userId ? new ModelCallerIdentity(userId, [.. permissions.Current]) : null;

    /// <inheritdoc />
    public async Task<Result<TextModelReply>> CallTextAsync(TextModelCall call, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);
        var result = await gateway.CallAsync(
            new AiCallRequest
            {
                Caller = new AiCaller(call.Caller.UserId, call.Caller.Permissions),
                ModelId = call.ModelId,
                Purpose = call.Role switch
                {
                    ModelRole.TopicSuggestion => CallPurpose.TopicSuggestion,
                    ModelRole.DuplicateCheck => CallPurpose.Review,
                    ModelRole.VideoAnalysis or ModelRole.VideoCode or ModelRole.CodeFix => CallPurpose.VideoCode,
                    _ => CallPurpose.ConceptGeneration,
                },
                ProfileId = call.ProfileId,
                Effort = call.Effort,
                System = call.System,
                Prompt = call.Prompt,
            },
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Result<TextModelReply>.Success(new TextModelReply(result.Value.Text, result.Value.Outcome == CallOutcome.Truncated, result.Value.CostUsd))
            : Result<TextModelReply>.Failure(result.Error!);
    }

    /// <inheritdoc />
    public async Task<Result<ImageModelReply>> CallImageAsync(ImageModelCall call, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);
        var result = await gateway.CallAsync(
            new AiCallRequest
            {
                Caller = new AiCaller(call.Caller.UserId, call.Caller.Permissions),
                ModelId = call.ModelId,
                Purpose = CallPurpose.Image,
                ProfileId = call.ProfileId,
                Prompt = call.Prompt,
                ImageCount = 1,
            },
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Result<ImageModelReply>.Success(new ImageModelReply(result.Value.Images.Count > 0 ? result.Value.Images[0] : null, result.Value.CostUsd))
            : Result<ImageModelReply>.Failure(result.Error!);
    }
}
