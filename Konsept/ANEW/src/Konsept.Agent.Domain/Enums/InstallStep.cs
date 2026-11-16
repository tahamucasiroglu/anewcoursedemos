namespace Konsept.Agent.Domain.Enums;

/// <summary>Kurulum/güncelleme ekranından açılan bildirimde hatanın adımı (spec 0022 Req 4; 0009 ve 0023 kullanır).</summary>
public enum InstallStep
{
    /// <summary>İndirme.</summary>
    Download = 0,

    /// <summary>Doğrulama.</summary>
    Verify = 1,

    /// <summary>Kurulum.</summary>
    Install = 2,
}
