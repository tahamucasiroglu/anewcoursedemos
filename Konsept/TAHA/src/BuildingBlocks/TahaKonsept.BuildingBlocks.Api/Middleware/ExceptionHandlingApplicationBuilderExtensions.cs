using Microsoft.AspNetCore.Builder;

namespace TahaKonsept.BuildingBlocks.Api.Middleware;

/// <summary>
/// <see cref="ExceptionHandlingMiddleware"/> kayıt uzantısı.
/// </summary>
public static class ExceptionHandlingApplicationBuilderExtensions
{
    /// <summary>Hata yakalama middleware'ini ekler (pipeline'ın başına yakın).</summary>
    /// <param name="app">Uygulama oluşturucu.</param>
    /// <returns>Aynı oluşturucu.</returns>
    public static IApplicationBuilder UseTahaExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
