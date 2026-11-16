using System.Collections.Concurrent;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Sahte sağlayıcı doğrulaması (testlerde gerçek sağlayıcı çağrısı yok): sonuç ayarlanır, her çağrı (sağlayıcı + key) kaydedilir.
/// HTTP eşlemesinin kendisi <c>ProviderKeyValidatorTests</c>'te sahte HTTP işleyicisiyle kanıtlanır.
/// </summary>
public sealed class FakeProviderKeyValidator : IProviderKeyValidator
{
    private readonly ConcurrentQueue<(string Provider, string Key)> _calls = new();

    /// <summary>Gets or sets bir sonraki çağrıların sonucu.</summary>
    public KeyCheckOutcome Outcome { get; set; } = KeyCheckOutcome.Accepted;

    /// <summary>Gets yapılan çağrılar.</summary>
    public IReadOnlyCollection<(string Provider, string Key)> Calls => _calls;

    /// <inheritdoc />
    public Task<KeyCheckOutcome> CheckAsync(ProviderEntry provider, string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _calls.Enqueue((provider.Id, key));
        return Task.FromResult(Outcome);
    }
}
