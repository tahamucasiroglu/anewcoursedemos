namespace Konsept.Agent.Infrastructure.Setup;

/// <summary>
/// Ajanın klasörleri (spec 0009 Req 6, 9, 22): her şey kullanıcı başına <c>%LOCALAPPDATA%\anewconcept</c> altında (yönetici izni
/// gerekmez; ajan exe'si nerede durursa dursun); varsayılan video klasörü <c>Videolar\anewconcept</c>.
/// </summary>
public static class AgentPaths
{
    /// <summary>Teknik ad (klasör, kayıt adları).</summary>
    public const string TechnicalName = "anewconcept";

    /// <summary>Gets kurulum yeri.</summary>
    public static string InstallRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), TechnicalName);

    /// <summary>Gets varsayılan video klasörü.</summary>
    public static string DefaultVideoFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), TechnicalName);
}
