using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence;

namespace TahaKonsept.Modules.Diagnostics.Infrastructure.Logging;

/// <summary>
/// <see cref="Application.Abstract.IActivityLogger"/>'ın EF uygulaması (singleton). Her yazım yeni bir DI scope'unda kendi
/// <see cref="DiagnosticsDbContext"/>'iyle yapılır; böylece çağıranın DbContext'ine karışmaz, paralel çağrılar güvenlidir ve
/// arka plan servislerinden de kullanılabilir. Oturum kullanıcısı scope içindeki <see cref="ICurrentUser"/>'dan (HTTP isteği varsa) okunur.
/// </summary>
public sealed class ActivityLogger : ActivityLoggerBase
{
    private readonly IServiceScopeFactory _scopes;

    /// <summary>Oluşturur.</summary>
    /// <param name="scopes">Scope fabrikası.</param>
    /// <param name="logger">Teknik günlükçü.</param>
    public ActivityLogger(IServiceScopeFactory scopes, ILogger<ActivityLogger> logger)
        : base(logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
    }

    /// <summary>Kaydı ayrı scope'ta veritabanına yazar.</summary>
    /// <param name="entry">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected override async Task WriteAsync(ActivityLog entry, CancellationToken cancellationToken)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagnosticsDbContext>();
        db.ActivityLogs.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Oturum kullanıcısı (HTTP isteği dışında <c>null</c>).</summary>
    /// <returns>Kullanıcı kimliği.</returns>
    protected override Guid? ResolveCurrentUserId()
    {
        using var scope = _scopes.CreateScope();
        return scope.ServiceProvider.GetService<ICurrentUser>()?.UserId;
    }
}
