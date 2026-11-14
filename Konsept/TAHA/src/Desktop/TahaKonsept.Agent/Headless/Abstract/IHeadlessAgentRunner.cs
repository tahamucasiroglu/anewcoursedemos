namespace TahaKonsept.Agent.Headless.Abstract;

/// <summary>
/// Penceresiz ajan çalıştırıcısı (geliştirici/e2e modu): sunucu adresini kaydeder, eksik ön gereksinimleri kurar, çekirdeği başlatır,
/// eşlenmemişse kodu dosyaya yazıp onayı bekler; oturum kapanırsa (ör. web'den "tüm oturumları kapat") yeniden eşleme ister.
/// Önemli üye: <see cref="RunAsync"/>.
/// </summary>
public interface IHeadlessAgentRunner
{
    /// <summary>İptal edilene kadar çalışır.</summary>
    /// <param name="options">Seçenekler.</param>
    /// <param name="ct">İptal belirteci (Ctrl+C / süreç sonu).</param>
    /// <returns>Çıkış kodu (0 = normal kapanış).</returns>
    Task<int> RunAsync(HeadlessLaunchOptions options, CancellationToken ct);
}
