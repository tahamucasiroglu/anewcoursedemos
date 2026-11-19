namespace OkulYonetim.WebAPI.Cors;

/// <summary>
/// Tarayıcı erişim izni (CORS): yalnızca "Cors:AllowedOrigins" ayarındaki web adreslerine izin verilir
/// (varsayılan reddet). Ayar yoksa yerel web paneli adresi kullanılır.
/// </summary>
public static class CorsSetup
{
    public const string PolicyName = "OkulWeb";
    private static readonly string[] DefaultOrigins = ["http://localhost:5173"];

    public static IServiceCollection AddOkulCors(this IServiceCollection services, IConfiguration configuration)
    {
        string[] origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? DefaultOrigins;
        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader()
            .WithExposedHeaders("X-Trace-Id")));
        return services;
    }
}
