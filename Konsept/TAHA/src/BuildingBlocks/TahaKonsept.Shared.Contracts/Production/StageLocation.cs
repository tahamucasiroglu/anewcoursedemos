namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Bir aşamanın nerede çalıştığı.
/// </summary>
public enum StageLocation
{
    /// <summary>API sunucusunda (Pi) çalışır; online sağlayıcılar.</summary>
    Server = 0,

    /// <summary>Kullanıcının bilgisayarındaki masaüstü ajanında çalışır.</summary>
    Agent = 1,
}
