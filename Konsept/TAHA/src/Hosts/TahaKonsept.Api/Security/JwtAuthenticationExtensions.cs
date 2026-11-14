using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Api.Security;

/// <summary>
/// JWT Bearer doğrulaması: imza anahtarı <see cref="IJwtSigningKeyProvider"/>'dan (<c>Jwt:SigningKey</c> ya da <c>App_Data/keys/jwt.key</c>),
/// issuer/audience <see cref="JwtOptions"/>'tan. <c>MapInboundClaims=false</c> (claim adları JWT'deki gibi: <c>sub</c>, <c>name</c>, <c>permission</c>).
/// SignalR için <c>/hubs</c> altındaki isteklerde token <c>access_token</c> sorgu parametresinden okunur.
/// </summary>
internal static class JwtAuthenticationExtensions
{
    /// <summary>JWT kimlik doğrulamasını ekler.</summary>
    /// <param name="services">Servisler.</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddTahaJwtAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IJwtSigningKeyProvider, JwtOptions>((options, keyProvider, jwt) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyProvider.GetSigningKey()),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = CustomClaimTypes.UserName,
                    RoleClaimType = "role",
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query[AgentHubRoutes.AccessTokenQueryKey];
                        if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        return services;
    }
}
