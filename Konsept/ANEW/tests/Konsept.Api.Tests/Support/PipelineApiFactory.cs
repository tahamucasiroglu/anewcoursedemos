namespace Konsept.Api.Tests.Support;

/// <summary>
/// Üretim hattı testleri için hızlı API (plan 0017): <see cref="ProductionApiFactory"/> ile aynı bellek depoları, sahte sağlayıcı ve sahte ajan
/// kanalı; hat (koşucu + zamanlayıcı) açıktır. Gerçek LLM, gerçek ajan ya da render yoktur.
/// </summary>
public sealed class PipelineApiFactory : ProductionApiFactory
{
    /// <inheritdoc />
    protected override bool PipelineEnabled => true;
}
