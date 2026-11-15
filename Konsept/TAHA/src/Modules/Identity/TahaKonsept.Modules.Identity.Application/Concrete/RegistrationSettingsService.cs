using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Base;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Domain.Settings;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Concrete;

/// <summary>
/// Kayıt ayarı (<see cref="IdentitySettingKeys.RegistrationOpen"/>). Satır yoksa kayıt açık kabul edilir; yönetici değiştirince
/// satır eklenir/güncellenir (eşzamanlı ilk eklemede kaybeden taraf mevcut satırı günceller) ve loglanır.
/// </summary>
public sealed class RegistrationSettingsService : RegistrationSettingsServiceBase
{
    private readonly ILogger<RegistrationSettingsService> _logger;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="logger">Günlükleyici.</param>
    public RegistrationSettingsService(IIdentitySettingRepository settings, ICurrentUser currentUser, IClock clock, ILogger<RegistrationSettingsService> logger)
        : base(settings, currentUser, clock)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<bool> IsRegistrationOpenAsync(CancellationToken ct = default)
    {
        var row = await Settings.FindAsync(IdentitySettingKeys.RegistrationOpen, ct);
        return ParseBool(row?.Value, fallback: true);
    }

    /// <inheritdoc />
    public override async Task<Result<RegistrationSettingsDto>> GetAsync(CancellationToken ct = default)
    {
        if (RequireAdmin() is { } denied)
        {
            return denied;
        }

        return new RegistrationSettingsDto { RegistrationOpen = await IsRegistrationOpenAsync(ct) };
    }

    /// <inheritdoc />
    public override async Task<Result<RegistrationSettingsDto>> UpdateAsync(RegistrationSettingsDto settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (RequireAdmin() is { } denied)
        {
            return denied;
        }

        var value = ToText(settings.RegistrationOpen);
        var row = await Settings.FindAsync(IdentitySettingKeys.RegistrationOpen, ct);
        if (row is null && !await Settings.TryAddAsync(new IdentitySetting(IdentitySettingKeys.RegistrationOpen, value, Clock.UtcNow), ct))
        {
            row = await Settings.FindAsync(IdentitySettingKeys.RegistrationOpen, ct);
        }

        if (row is not null)
        {
            row.Update(value, Clock.UtcNow);
            await Settings.SaveAsync(ct);
        }

        _logger.LogInformation("Yeni kayıtlar {State} (yönetici {UserId}).", settings.RegistrationOpen ? "açıldı" : "kapatıldı", CurrentUser.UserId);
        return new RegistrationSettingsDto { RegistrationOpen = settings.RegistrationOpen };
    }
}
