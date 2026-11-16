namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>Spec'te sabitlenen giriş sınırları (spec 0014 Req 2, 8; G-5): adet 1–100, fikir en fazla 2.000 karakter; liste 50'şerli (G-14).</summary>
public static class ConceptLimits
{
    /// <summary>En az adet.</summary>
    public const int MinCount = 1;

    /// <summary>En fazla adet.</summary>
    public const int MaxCount = 100;

    /// <summary>Fikir metninin en fazla uzunluğu.</summary>
    public const int MaxIdeaLength = 2000;

    /// <summary>Üretilenler sayfa boyutu.</summary>
    public const int PageSize = 50;
}
