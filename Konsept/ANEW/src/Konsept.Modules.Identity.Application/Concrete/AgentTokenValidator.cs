using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Base;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Ajan belirteci doğrulayıcı.</summary>
/// <param name="devices">Cihaz deposu.</param>
/// <param name="tokens">Belirteç hash'i.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class AgentTokenValidator(IAgentDeviceRepository devices, IRefreshTokenFactory tokens, TimeProvider timeProvider)
    : AgentTokenValidatorBase(devices, tokens, timeProvider);
