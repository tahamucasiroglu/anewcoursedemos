namespace TahaKonsept.Agent.WebSite.Abstract;

/// <summary>"Siteye git" için web arayüzü adresini verir (ayarda yoksa API adresinden türetilir).</summary>
public interface IWebSiteLocator
{
    /// <summary>Web arayüzünün adresi.</summary>
    /// <returns>Mutlak adres (ör. <c>http://192.168.1.102:7110/</c>).</returns>
    string GetWebUrl();
}
