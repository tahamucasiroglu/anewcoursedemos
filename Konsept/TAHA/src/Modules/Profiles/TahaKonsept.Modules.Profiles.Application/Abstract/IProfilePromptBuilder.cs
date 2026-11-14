using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Abstract;

/// <summary>
/// Profili, LLM sistem promptuna eklenecek kısa ve token-optimize bir Türkçe kanal tanımına çevirir (PUBLIC sözleşme).
/// Boş alanlar atlanır, boşluklar sıkıştırılır, uzun kanal içeriği kelime sınırında kısaltılır.
/// </summary>
public interface IProfilePromptBuilder
{
    /// <summary>Profilden sistem promptu parçası üretir.</summary>
    /// <param name="profile">Profil.</param>
    /// <returns>Çok satırlı, kısa Türkçe metin.</returns>
    string Build(ProfileDto profile);
}
