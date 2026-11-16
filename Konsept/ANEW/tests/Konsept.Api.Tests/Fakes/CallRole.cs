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

    /// <summary>Üretim hattı: analiz (plan 0017).</summary>
    Analysis,

    /// <summary>Üretim hattı: sahne video kodu.</summary>
    Code,

    /// <summary>Üretim hattı: render düzeltmesi.</summary>
    Fix,

    /// <summary>Üretim hattı: online görsel.</summary>
    Image,

    /// <summary>Diğer.</summary>
    Other,
}
