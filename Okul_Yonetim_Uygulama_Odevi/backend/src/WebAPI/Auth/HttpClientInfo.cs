using OkulYonetim.Application.Ports;

namespace OkulYonetim.WebAPI.Auth;

/// <summary>İstemcinin IP adresi (vekil başlığı işlendikten sonra); HTTP isteği yoksa (komut satırı) boş.</summary>
public sealed class HttpClientInfo : IClientInfo
{
    private readonly IHttpContextAccessor accessor;

    public HttpClientInfo(IHttpContextAccessor accessor)
    {
        this.accessor = accessor;
    }

    public string? IpAddress => this.accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
