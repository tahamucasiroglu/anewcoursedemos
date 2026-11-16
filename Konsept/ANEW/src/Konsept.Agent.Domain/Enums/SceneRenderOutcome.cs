namespace Konsept.Agent.Domain.Enums;

/// <summary>Bir sahnenin kısıtlı render sonucu (spec 0018).</summary>
public enum SceneRenderOutcome
{
    /// <summary>Klip üretildi ve çıktı kapısından geçti.</summary>
    Ok,

    /// <summary>Motor hata verdi (düzeltme döngüsüne girer).</summary>
    Failed,

    /// <summary>Süre, bellek ya da süreç sınırı aşıldı; ağaç sonlandırıldı.</summary>
    LimitExceeded,

    /// <summary>Çıktı beklenen biçimde değil (oynatılamıyor, boyut/süre sapıyor).</summary>
    OutputInvalid,

    /// <summary>İş klasörü güvenli değil (klasör dışını gösteren bağlantı) ya da kod yasaklı yapı içeriyor.</summary>
    Unsafe,
}
