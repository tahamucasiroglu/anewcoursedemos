namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Kullanıcının eşleşmiş ajan cihazlarını iptal eder ve dinleyicilere (AgentGateway) bildirir (spec 0012 Req 6, 12).
/// </summary>
public interface IAgentDeviceRevoker
{
    /// <summary>Kullanıcının etkin tüm cihazlarını iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İptal edilen cihaz sayısı.</returns>
    Task<int> RevokeAllAsync(Guid userId, CancellationToken cancellationToken);
}
