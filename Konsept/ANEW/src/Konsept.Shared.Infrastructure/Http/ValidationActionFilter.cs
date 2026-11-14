using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Konsept.Shared.Infrastructure.Http;

/// <summary>
/// Generic doğrulama filtresi: eylem argümanlarının her biri için kayıtlı bir FluentValidation
/// <see cref="IValidator{T}"/> varsa çalıştırır; hata varsa eylem çağrılmadan 400 döner (K-4).
/// </summary>
public sealed class ValidationActionFilter : IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        foreach (var argument in context.ActionArguments.Values.OfType<object>())
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validation = await validator
                .ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted)
                .ConfigureAwait(false);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray(), StringComparer.Ordinal);
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
                {
                    Title = "Girilen bilgiler geçersiz.",
                });
                return;
            }
        }

        await next().ConfigureAwait(false);
    }
}
