using FluentValidation;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Application.Resources;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.AiGateway.Domain.Rules;

namespace Konsept.Modules.AiGateway.Application.Validators;

/// <summary>
/// API key biçim kuralları (spec 0004 Req 8, <see cref="ApiKeyFormat"/>): boş, 8'den kısa, 512'den uzun ya da kontrol karakterli
/// değer Türkçe mesajla reddedilir. Mesajlar girilen değeri içermez.
/// </summary>
public sealed class SaveApiKeyRequestValidator : AbstractValidator<SaveApiKeyRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SaveApiKeyRequestValidator"/> class.</summary>
    public SaveApiKeyRequestValidator()
    {
        RuleFor(r => r.ApiKey).Custom((value, context) =>
        {
            if (MessageFor(ApiKeyFormat.Check(value)) is { } message)
            {
                context.AddFailure(new FluentValidation.Results.ValidationFailure(nameof(SaveApiKeyRequest.ApiKey), message) { ErrorCode = "ai.key_invalid" });
            }
        });
    }

    /// <summary>Biçim sorununun Türkçe mesajı.</summary>
    /// <param name="issue">Sorun.</param>
    /// <returns>Mesaj; sorun yoksa <c>null</c>.</returns>
    public static string? MessageFor(ApiKeyFormatIssue issue) => issue switch
    {
        ApiKeyFormatIssue.Empty => AiGatewayText.ApiKeyRequired,
        ApiKeyFormatIssue.TooShort => AiGatewayText.ApiKeyTooShort,
        ApiKeyFormatIssue.TooLong => AiGatewayText.ApiKeyTooLong,
        ApiKeyFormatIssue.ControlCharacter => AiGatewayText.ApiKeyControlCharacter,
        _ => null,
    };
}
