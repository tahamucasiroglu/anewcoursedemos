using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Domain.Rules;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Ajan belirteci doğrulaması (spec 0012 Req 9–10): hash'le cihaz bulunur; iptal edilmiş ya da 90 gün kullanılmamışsa
/// reddedilir; geçerliyse son kullanım en fazla dakikada bir kaydırılır (ajan çalıştıkça oturum yenilenir).
/// </summary>
/// <param name="devices">Cihaz deposu.</param>
/// <param name="tokens">Belirteç hash'i.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public abstract class AgentTokenValidatorBase(IAgentDeviceRepository devices, IRefreshTokenFactory tokens, TimeProvider timeProvider) : IAgentTokenValidator
{
    /// <inheritdoc />
    public async Task<AgentPrincipal?> ValidateAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var device = await devices.FindByTokenHashAsync(tokens.Hash(token), cancellationToken).ConfigureAwait(false);
        if (device is null || !device.IsActive(now))
        {
            return null;
        }

        if (SessionPolicy.ShouldTouch(device.LastUsedAt, now))
        {
            await devices.TouchAsync(device.Id, now, cancellationToken).ConfigureAwait(false);
        }

        return new AgentPrincipal(device.UserId, device.Id);
    }
}
