namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Günlük kayıtlarının (<c>ActivityLog.Source</c>) önerilen kaynak adları. Kaynak serbest metindir (en çok 64 karakter);
/// modüller kendi adlarını da kullanabilir, ama aynı yer için hep aynı ad kullanılmalıdır.
/// </summary>
public static class ActivitySources
{
    /// <summary>Web arayüzü (tarayıcı olayları).</summary>
    public const string Web = "Web";

    /// <summary>API sunucusu.</summary>
    public const string Api = "Api";

    /// <summary>Masaüstü ajanı.</summary>
    public const string Agent = "Agent";

    /// <summary>Masaüstü kurulum ekranı.</summary>
    public const string Installer = "Installer";

    /// <summary>Video üretim hattı.</summary>
    public const string Production = "Production";

    /// <summary>Konsept üretimi.</summary>
    public const string Concepts = "Concepts";

    /// <summary>Kimlik / oturum.</summary>
    public const string Identity = "Identity";

    /// <summary>Sağlayıcılar (LLM, ses, görsel).</summary>
    public const string Providers = "Providers";

    /// <summary>Tanılama / yönetim paneli.</summary>
    public const string Diagnostics = "Diagnostics";

    /// <summary>Sistem (kullanıcısız arka plan işleri).</summary>
    public const string System = "System";
}
