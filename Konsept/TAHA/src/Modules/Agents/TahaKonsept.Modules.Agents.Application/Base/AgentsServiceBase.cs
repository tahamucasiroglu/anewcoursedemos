using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Mapping;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Base;

/// <summary>
/// Agents uygulama servislerinin ortak tabanı: saat (<see cref="Clock"/>), oturum kullanıcısı zorunluluğu (<see cref="RequireUser"/>),
/// cihaz → <see cref="AgentDeviceInfo"/> dönüşümü (<see cref="ToInfo"/>) ve yetenek JSON'u yazma (<see cref="WriteCapabilities"/>).
/// </summary>
public abstract class AgentsServiceBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="clock">Saat.</param>
    protected AgentsServiceBase(IClock clock)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Oturum yok hatası.</summary>
    protected static Error UnauthorizedError { get; } = Error.Unauthorized("agents.unauthorized", "Bu işlem için oturum açmalısınız.");

    /// <summary>Saat.</summary>
    protected IClock Clock { get; }

    /// <summary>Şu an (UTC).</summary>
    protected DateTimeOffset Now => Clock.UtcNow;

    /// <summary>Oturumdaki kullanıcı kimliğini ya da yetkisiz hatasını döner.</summary>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    /// <returns>Kullanıcı kimliği.</returns>
    protected static Result<Guid> RequireUser(ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        return currentUser.UserId is { } id && id != Guid.Empty ? id : UnauthorizedError;
    }

    /// <summary>Cihazı API modeline çevirir.</summary>
    /// <param name="device">Cihaz.</param>
    /// <param name="isOnline">Canlı çevrimiçi durumu.</param>
    /// <returns>Model.</returns>
    protected static AgentDeviceInfo ToInfo(AgentDevice device, bool isOnline) => AgentDeviceMapper.ToInfo(device, isOnline);

    /// <summary>Yetenekleri JSON'a çevirir.</summary>
    /// <param name="capabilities">Yetenekler.</param>
    /// <returns>JSON ya da <c>null</c>.</returns>
    protected static string? WriteCapabilities(AgentCapabilities? capabilities) => AgentDeviceMapper.WriteCapabilities(capabilities);
}
