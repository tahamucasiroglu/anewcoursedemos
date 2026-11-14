using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Agent.Core.Auth.Abstract;

/// <summary>
/// Ajanın erişim/yenileme token çiftini diskte şifreli saklar (Windows'ta DPAPI, CurrentUser kapsamı).
/// </summary>
public interface ITokenStore
{
    /// <summary>Kayıtlı token'ları okur; yoksa ya da çözülemiyorsa <c>null</c>.</summary>
    /// <returns>Token çifti ya da <c>null</c>.</returns>
    TokenResponse? Load();

    /// <summary>Token'ları şifreleyip kaydeder.</summary>
    /// <param name="tokens">Token çifti.</param>
    void Save(TokenResponse tokens);

    /// <summary>Kayıtlı token'ları siler.</summary>
    void Clear();
}
