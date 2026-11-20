using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Application.Pipelines.Validation;
using NArchitecture.Core.Mediator.DependencyInjection;
using OkulYonetim.Application.Features.Identity.Sessions;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application;

/// <summary>
/// Application katmanının servis kaydı: nArchitecture Mediator (handler taraması), doğrulama pipeline'ı (FluentValidation
/// doğrulayıcıları), işlem log pipeline'ı (<c>ILoggableRequest</c> işaretli istekler loglanır) ve istek başına oturum
/// bilgisi (<see cref="CurrentSession"/>; filtreler buradan okur).
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        services.AddMediator(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(typeof(ApplicationServiceRegistration).Assembly);
            configuration.AddOpenBehavior(typeof(RequestValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CurrentSession>();
        services.AddScoped<ICurrentSession>(provider => provider.GetRequiredService<CurrentSession>());
        services.AddScoped<SessionAuthenticator>();
        services.AddScoped<UserTenantDirectory>();
        services.AddScoped<Features.Administration.AdminGuard>();
        services.AddScoped<Features.Administration.AdminAudit>();
        services.AddScoped<Features.Administration.MemberAccounts>();
        services.AddScoped<Features.Students.StudentHealth>();
        services.AddSingleton<Features.Organization.SchoolCalendar>();
        return services;
    }
}
