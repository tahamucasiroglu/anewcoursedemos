namespace Konsept.Modules.Production.Domain.Enums;

/// <summary>
/// Üretim hattının aşamaları, çalışma sırasıyla (spec 0017 Req 6): Konsept inceleniyor (analiz) → Video kodu → Görseller → Seslendirme →
/// Render → Birleştirme. İlk ikisi online (Pi), görseller seçime göre online ya da yerel, sonrakiler ajanda.
/// </summary>
public enum PipelineStage
{
    /// <summary>Konsept inceleniyor.</summary>
    Analysis = 0,

    /// <summary>Video kodu üretiliyor (sahne başına).</summary>
    Code = 1,

    /// <summary>Görseller üretiliyor.</summary>
    Images = 2,

    /// <summary>Seslendirme yapılıyor.</summary>
    Voice = 3,

    /// <summary>Video kodu videoya dönüştürülüyor (render).</summary>
    Render = 4,

    /// <summary>Birleştiriliyor.</summary>
    Merge = 5,
}
