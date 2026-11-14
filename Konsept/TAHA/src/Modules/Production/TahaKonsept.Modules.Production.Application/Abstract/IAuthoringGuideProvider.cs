namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Video motoru yazım kılavuzlarını (LLM sistem promptu) sağlar. Altyapıdaki uygulama <c>AuthoringGuides.ForEngine</c> kullanır.
/// </summary>
public interface IAuthoringGuideProvider
{
    /// <summary>Motorun kılavuzu (bilinmeyen motor için <c>null</c>). <c>{W}</c>/<c>{H}</c>/<c>{D}</c> yer tutucuları doldurulmamış haldedir.</summary>
    /// <param name="engineKey">Motor anahtarı.</param>
    /// <returns>Sonuç.</returns>
    string? GetGuide(string engineKey);
}
