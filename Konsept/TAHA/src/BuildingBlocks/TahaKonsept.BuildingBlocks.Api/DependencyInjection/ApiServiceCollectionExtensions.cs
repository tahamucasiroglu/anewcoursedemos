using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.BuildingBlocks.Api.Filters;
using TahaKonsept.BuildingBlocks.Api.Security.Concrete;
using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.BuildingBlocks.Api.DependencyInjection;

/// <summary>
/// BuildingBlocks API servislerinin kaydı: HttpContext erişimi, <see cref="ICurrentUser"/> (<see cref="HttpCurrentUser"/>),
/// global <see cref="ValidationFilter"/> ve ProblemDetails.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>API ortak servislerini ekler.</summary>
    /// <param name="services">Servisler.</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddBuildingBlocksApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpCurrentUser>();
        services.TryAddScoped<ValidationFilter>();
        services.AddProblemDetails();
        services.Configure<MvcOptions>(options => options.Filters.AddService<ValidationFilter>());
        return services;
    }
}
