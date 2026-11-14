namespace TahaKonsept.Web.Models.Api;

/// <summary>Kurulum durumu (<c>GET api/identity/setup/status</c>): veritabanında hiç kullanıcı yoksa <see cref="SetupRequired"/> true.</summary>
public sealed record SetupStatusResponse
{
    /// <summary>İlk yönetici hesabı henüz oluşturulmadıysa true.</summary>
    public bool SetupRequired { get; init; }
}
