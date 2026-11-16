using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Eşleştirme uçlarının ajan tarafı (spec 0012 Req 1, 10): kod alma ve kod durumu sorgusu (giriş istemez). Sunucuya
/// ulaşılamazsa <see cref="HttpRequestException"/> fırlatır.
/// </summary>
public interface IPairingClient
{
    /// <summary>Yeni kod alır.</summary>
    /// <param name="machineName">Makine adı.</param>
    /// <param name="agentVersion">Ajan sürümü.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kod.</returns>
    Task<PairingCodeInfo> RequestCodeAsync(string machineName, string agentVersion, CancellationToken cancellationToken);

    /// <summary>Kodun durumunu sorar.</summary>
    /// <param name="secret">Onay sırrı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<PairingPoll> CheckAsync(string secret, CancellationToken cancellationToken);
}
