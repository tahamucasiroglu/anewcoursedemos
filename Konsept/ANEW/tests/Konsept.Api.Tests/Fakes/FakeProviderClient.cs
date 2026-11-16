using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Sahte sağlayıcı istemcisi (plan 0013; gerçek sağlayıcı çağrısı yok). Her denemede sıradaki yanıtı (<see cref="Enqueue"/>) döndürür,
/// sıra boşsa <see cref="Default"/>'u. <see cref="Hang"/> açıkken iptal edilene (zaman aşımına) kadar bekler; <see cref="Gate"/>
/// verilmişse yanıt vermeden önce onu bekler. Her çağrı, zamanıyla birlikte <see cref="Calls"/>'a eklenir.
/// </summary>
/// <param name="time">Çağrı zamanı için saat.</param>
public sealed class FakeProviderClient(TimeProvider? time = null) : IProviderClient
{
    private readonly Lock _gate = new();
    private readonly Queue<ProviderAttempt> _queue = new();
    private readonly List<(ProviderCall Call, DateTimeOffset At)> _calls = [];

    /// <summary>Gets or sets sıra boşken dönen yanıt.</summary>
    public ProviderAttempt Default { get; set; } = ProviderAttempt.Completed("Merhaba, ben bir modelim.", 120, 30);

    /// <summary>Gets or sets a value indicating whether yanıt yerine iptale kadar beklenir.</summary>
    public bool Hang { get; set; }

    /// <summary>Gets or sets yanıttan önce beklenen görev (key silme yarışı için).</summary>
    public Task? Gate { get; set; }

    /// <summary>Gets yapılan çağrılar ve zamanları.</summary>
    public IReadOnlyList<(ProviderCall Call, DateTimeOffset At)> Calls
    {
        get
        {
            lock (_gate)
            {
                return [.. _calls];
            }
        }
    }

    /// <summary>Sıraya yanıt ekler.</summary>
    /// <param name="attempts">Yanıtlar.</param>
    public void Enqueue(params ProviderAttempt[] attempts)
    {
        lock (_gate)
        {
            foreach (var attempt in attempts)
            {
                _queue.Enqueue(attempt);
            }
        }
    }

    /// <summary>Çağrıları ve sırayı temizler.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _calls.Clear();
            _queue.Clear();
        }

        Hang = false;
        Gate = null;
    }

    /// <inheritdoc />
    public async Task<ProviderAttempt> SendAsync(ProviderCall call, CancellationToken cancellationToken)
    {
        ProviderAttempt next;
        lock (_gate)
        {
            _calls.Add((call, (time ?? TimeProvider.System).GetUtcNow()));
            next = _queue.Count > 0 ? _queue.Dequeue() : Default;
        }

        if (Hang)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        if (Gate is { } gate)
        {
            await gate.WaitAsync(cancellationToken);
        }

        return next;
    }
}
