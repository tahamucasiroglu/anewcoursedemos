namespace OkulYonetim.Application.Ports;

/// <summary>
/// Gönderilecek e-posta. Gövde bağlantı (gizli anahtar) taşıyabilir: bu kayıt ve gövdesi hiçbir zaman loglanmaz; loglarda
/// yalnızca <see cref="UserId"/> geçer (spec 0006 gereksinim 8–9).
/// </summary>
public sealed record EmailMessage(string ToAddress, string ToName, string Subject, string Body, Guid? UserId);
