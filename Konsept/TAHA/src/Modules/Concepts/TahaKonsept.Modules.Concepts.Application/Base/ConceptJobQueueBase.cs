using System.Threading.Channels;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Models;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptJobQueue"/> ortak davranışı: sınırsız, tek okuyuculu <see cref="Channel{T}"/> (FIFO → işler sırayla).
/// </summary>
public abstract class ConceptJobQueueBase : IConceptJobQueue
{
    private readonly Channel<ConceptJob> _channel = Channel.CreateUnbounded<ConceptJob>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    /// <summary>İşi ekler.</summary>
    /// <param name="job">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public ValueTask EnqueueAsync(ConceptJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        return _channel.Writer.WriteAsync(job, cancellationToken);
    }

    /// <summary>İşleri okur.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş akışı.</returns>
    public IAsyncEnumerable<ConceptJob> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}
