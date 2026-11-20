namespace OkulYonetim.Application.Features.Platform;

/// <summary>Okulun kurucusu (destek için): ad-soyad ve e-posta.</summary>
public sealed record SchoolFounder(string FullName, string Email);
