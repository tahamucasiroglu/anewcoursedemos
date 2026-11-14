namespace TahaKonsept.Agent.Core.VideoServer;

/// <summary><c>Range</c> başlığı çözüm sonucu türü.</summary>
public enum RangeParseOutcome
{
    /// <summary>Başlık yok/geçersiz/çoklu aralık: tüm dosya (200).</summary>
    Full = 0,

    /// <summary>Tek geçerli aralık (206).</summary>
    Partial = 1,

    /// <summary>Aralık dosya dışında (416).</summary>
    Unsatisfiable = 2,
}
