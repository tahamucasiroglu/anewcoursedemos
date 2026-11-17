using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>Dış süreç çalıştırma (spec 0028 Req 10): pencere açmadan, süre sınırıyla; iptal ve süre aşımında süreç ağacı öldürülür. Testlerde sahte.</summary>
public interface IProcessRunner
{
    /// <summary>Süreci çalıştırır ve bitmesini bekler.</summary>
    /// <param name="spec">Süreç.</param>
    /// <param name="cancellationToken">Durdur belirteci (iptalde <see cref="OperationCanceledException"/>).</param>
    /// <returns>Sonuç (başlatılamazsa çıkış kodu -1).</returns>
    Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken);
}
