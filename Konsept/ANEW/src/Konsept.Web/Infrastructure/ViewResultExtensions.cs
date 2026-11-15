using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Infrastructure;

/// <summary>Sayfa sonuçları için yardımcılar; controller eylemleri tek satır kalır (K-4).</summary>
public static class ViewResultExtensions
{
    /// <summary>Sayfayı verilen HTTP durum koduyla döndürür (ör. yetkisiz sayfa 403).</summary>
    /// <param name="view">Sayfa sonucu.</param>
    /// <param name="statusCode">Durum kodu.</param>
    /// <returns>Aynı sonuç.</returns>
    public static ViewResult WithStatusCode(this ViewResult view, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(view);
        view.StatusCode = statusCode;
        return view;
    }
}
