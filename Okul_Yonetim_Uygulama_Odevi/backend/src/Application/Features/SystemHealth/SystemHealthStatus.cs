namespace OkulYonetim.Application.Features.SystemHealth;

/// <summary>Sistem durumu sorgusunun iki olası sonucu: çalışıyor (veritabanına ulaşıldı) veya hizmet dışı.</summary>
public enum SystemHealthStatus
{
    Healthy = 0,
    Unavailable = 1,
}
