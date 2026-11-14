using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.BuildingBlocks.Api.Modules.Base;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Concrete;
using TahaKonsept.Modules.Diagnostics.Application.Validation;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Logging;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence.Repositories;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Users;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Api;

/// <summary>
/// Diagnostics modül kaydı: <see cref="DiagnosticsDbContext"/> (şema <c>diagnostics</c>), depolar, servisler, doğrulayıcılar,
/// singleton <see cref="IActivityLogger"/>, kendi <see cref="IStatsContributor"/>'ı ve Identity'nin <c>IUserDirectory</c>'sini saran
/// <see cref="IAdminUserDirectory"/> (<see cref="IdentityAdminUserDirectory"/>; Identity modülü kayıtlı olmalı).
/// Diğer modüller: <c>services.AddScoped&lt;IStatsContributor, XStatsContributor&gt;()</c>.
/// </summary>
public sealed class DiagnosticsModule : ModuleBase
{
    /// <summary>Modül adı.</summary>
    public override string Name => "Diagnostics";

    /// <summary>Servisleri kaydeder.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<DiagnosticsDbContext>(configuration, DiagnosticsDbContext.SchemaName);

        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IErrorReportRepository, ErrorReportRepository>();

        services.AddSingleton<IActivityLogger, ActivityLogger>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IErrorReportService, ErrorReportService>();
        services.AddScoped<IAdminStatsService, AdminStatsService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminUserDirectory, IdentityAdminUserDirectory>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStatsContributor, DiagnosticsStatsContributor>());

        services.AddScoped<IValidator<ActivityLogCreateRequest>, ActivityLogCreateRequestValidator>();
        services.AddScoped<IValidator<ErrorReportCreateRequest>, ErrorReportCreateRequestValidator>();
        services.AddScoped<IValidator<ErrorReportStatusUpdateRequest>, ErrorReportStatusUpdateRequestValidator>();
    }

    /// <summary>Migration'ları uygular.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        MigrateDbContextAsync<DiagnosticsDbContext>(services, cancellationToken);
}
