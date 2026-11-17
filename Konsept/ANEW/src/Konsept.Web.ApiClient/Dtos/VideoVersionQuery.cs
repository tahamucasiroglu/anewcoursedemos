namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Yeni sürüm / yeniden üret sorguları (spec 0020; taban sürüm ve bağlam belirteci sorgu dizesinde).</summary>
public enum VideoVersionQuery
{
    /// <summary>Yeni sürüm alanı: taban ayarları ve seçenekler (<c>GET</c>).</summary>
    Options,

    /// <summary>Yeniden üret bağlamını aç (<c>POST</c>).</summary>
    OpenRegeneration,

    /// <summary>Yeniden üretim önizlemesi (<c>GET</c>).</summary>
    RegenerationPreview,
}
