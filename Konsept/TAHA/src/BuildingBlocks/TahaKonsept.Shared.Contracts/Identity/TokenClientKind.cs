namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Token'ın verildiği istemci türü. Her tür kendi refresh token ailesini kullanır; çıkışta (logout-all) hepsi iptal edilir.
/// </summary>
public enum TokenClientKind
{
    /// <summary>Web (MVC BFF) oturumu.</summary>
    Web = 0,

    /// <summary>Masaüstü ajanı (cihaz eşleme ile).</summary>
    Agent = 1,
}
