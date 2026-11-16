namespace Konsept.Api.Tests.Fakes;

/// <summary>Sahte sağlayıcıya gelen çağrının rolü (talimattaki sabit görev cümlesinden; plan 0014).</summary>
public enum CallRole
{
    /// <summary>Konu önerisi.</summary>
    Topics,

    /// <summary>Konsept.</summary>
    Concept,

    /// <summary>Anlamca tekrar denetimi.</summary>
    Dedupe,

    /// <summary>Diğer.</summary>
    Other,
}
