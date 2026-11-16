using System.Globalization;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Infrastructure;

/// <summary>
/// Aylık harcamanın gösterimi (spec 0013 Req 15, AC-14, AC-18): yuvarlanmamış toplam yalnızca gösterimde iki haneye, yarım yukarı
/// (<see cref="MidpointRounding.AwayFromZero"/>) yuvarlanır ve Türkçe ondalık virgülle yazılır (<c>$2,37</c>); erişilebilir ad
/// "Bu ayki harcama: 2,37 dolar". Metinler resx'tendir.
/// </summary>
public static class SpendDisplay
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Tutarı iki haneli Türkçe biçime çevirir.</summary>
    /// <param name="usd">Yuvarlanmamış toplam.</param>
    /// <returns>Ör. <c>2,37</c>.</returns>
    public static string Amount(decimal usd) =>
        decimal.Round(usd, 2, MidpointRounding.AwayFromZero).ToString("0.00", Turkish);

    /// <summary>Api sonucunu göstergenin JSON'una çevirir (<c>text</c>, <c>label</c>); hata ortak tarayıcı JSON'udur.</summary>
    /// <param name="resultTask">Api sonucu.</param>
    /// <param name="http">İstek bağlamı.</param>
    /// <returns>JSON.</returns>
    public static async Task<IActionResult> ToSpendJson(this Task<Result<MonthlySpendResponse>> resultTask, HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        var result = await resultTask.ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return await Task.FromResult(result).ToBrowserJson(http).ConfigureAwait(false);
        }

        var amount = Amount(result.Value.TotalUsd);
        return new JsonResult(new
        {
            text = string.Format(Turkish, UiText.MonthlySpendValue, amount),
            label = string.Format(Turkish, UiText.MonthlySpendValueLabel, amount),
        });
    }
}
