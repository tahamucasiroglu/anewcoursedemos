namespace OkulYonetim.Application.Ports;

/// <summary>İsteği yapan istemcinin IP adresi (nginx arkasında gerçek adres); audit kaydı için. Komut satırında boştur.</summary>
public interface IClientInfo
{
    string? IpAddress { get; }
}
