using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TahaKonsept.Agent.Core.Networking.Abstract;

namespace TahaKonsept.Agent.Core.Networking.Base;

/// <summary>
/// LAN adresi bulucu tabanı: önce hedef sunucuya UDP "bağlanıp" (paket gönderilmez) işletim sisteminin seçtiği yerel adresi alır;
/// olmazsa ağ geçidi olan, çalışan, döngü dışı ilk arayüzün IPv4 adresini döner. Hedef sunucu <see cref="GetTargetHost"/> ile verilir.
/// </summary>
public abstract class LanAddressProviderBase : ILanAddressProvider
{
    /// <inheritdoc />
    public string GetLanIpAddress() => TryViaRoute() ?? TryViaInterfaces() ?? string.Empty;

    /// <summary>Yönlendirme için hedef ana makine (ör. sunucu IP'si); yoksa <c>null</c>.</summary>
    /// <returns>Ana makine.</returns>
    protected abstract string? GetTargetHost();

    private string? TryViaRoute()
    {
        var host = GetTargetHost();
        if (string.IsNullOrWhiteSpace(host) || !IPAddress.TryParse(host, out var target) || target.AddressFamily != AddressFamily.InterNetwork)
        {
            return null;
        }

        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(new IPEndPoint(target, 65530));
            return socket.LocalEndPoint is IPEndPoint local && !IPAddress.IsLoopback(local.Address) ? local.Address.ToString() : null;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static string? TryViaInterfaces()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                var properties = nic.GetIPProperties();
                if (properties.GatewayAddresses.Count == 0)
                {
                    continue;
                }

                var address = properties.UnicastAddresses
                    .Select(u => u.Address)
                    .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
                if (address is not null)
                {
                    return address.ToString();
                }
            }
        }
        catch (NetworkInformationException)
        {
            // Arayüzler okunamadı.
        }

        return null;
    }
}
