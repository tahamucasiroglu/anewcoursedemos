using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using TahaKonsept.BuildingBlocks.Api.Results;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.BuildingBlocks.Api.Filters;

/// <summary>
/// Genel FluentValidation filtresi (global kayıtlı): her aksiyon argümanı için DI'dan <c>IValidator&lt;T&gt;</c>
/// (T = argümanın türü) çözülür ve doğrulanır. Hata varsa aksiyon çalışmaz, 400 ProblemDetails
/// (<c>code = validation.failed</c>, <c>errors</c> = camelCase alan → mesajlar) döner. Controller'da validasyon kodu yazılmaz.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    /// <summary>Doğrulama hatası kodu.</summary>
    public const string ErrorCode = "validation.failed";

    /// <summary>Aksiyon öncesi doğrulama.</summary>
    /// <param name="context">Aksiyon bağlamı.</param>
    /// <param name="next">Sonraki adım.</param>
    /// <returns>Görev.</returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var services = context.HttpContext.RequestServices;
        var cancellationToken = context.HttpContext.RequestAborted;

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), cancellationToken);
            foreach (var failure in result.Errors)
            {
                var key = ToCamelCasePath(failure.PropertyName);
                if (!errors.TryGetValue(key, out var list))
                {
                    list = [];
                    errors[key] = list;
                }

                list.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            var details = errors.ToDictionary(p => p.Key, p => p.Value.ToArray(), StringComparer.Ordinal);
            context.Result = ProblemDetailsMapper.ToObjectResult(
                context.HttpContext,
                Error.Validation(ErrorCode, "Girilen bilgiler geçersiz.", details));
            return;
        }

        await next();
    }

    private static string ToCamelCasePath(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return string.Empty;
        }

        var parts = propertyName.Split('.');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = JsonNamingPolicy.CamelCase.ConvertName(parts[i]);
        }

        return string.Join('.', parts);
    }
}
