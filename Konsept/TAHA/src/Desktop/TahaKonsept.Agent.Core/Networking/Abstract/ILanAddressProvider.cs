namespace TahaKonsept.Agent.Core.Networking.Abstract;

/// <summary>Bu bilgisayarın yerel ağ IPv4 adresini bulur (web oynatıcısı video adresini bununla kurar).</summary>
public interface ILanAddressProvider
{
    /// <summary>Sunucuya giden yoldaki yerel IPv4 adresi; bulunamazsa ilk uygun arayüz adresi ya da boş metin.</summary>
    /// <returns>IP adresi metni.</returns>
    string GetLanIpAddress();
}
