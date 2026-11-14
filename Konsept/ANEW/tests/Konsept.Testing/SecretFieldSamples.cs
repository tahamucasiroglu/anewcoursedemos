namespace Konsept.Testing;

/// <summary>
/// AC-8 tam alan tablosu (spec 0008 Req 8, plan maskeleme tablosu): gizli alan adı, örnek değer ve çıktıda asla geçmemesi
/// gereken parça. Dosya, veritabanı ve ajan özeti testleri aynı tabloyu kullanır; yeni gizli alan buraya eklenir.
/// </summary>
public static class SecretFieldSamples
{
    /// <summary>Gets alan, değer ve sızmaması gereken parça.</summary>
    public static IReadOnlyList<(string Field, string Value, string Secret)> All { get; } =
    [
        ("Password", "hunter2-pw", "hunter2-pw"),
        ("ApiKey", "sk-proj-FIELDKEY123456-f1k", "FIELDKEY123456"),
        ("AccessToken", "acc-opaque-1", "acc-opaque-1"),
        ("RefreshToken", "ref-opaque-2", "ref-opaque-2"),
        ("PairingCode", "PAIR-778899", "PAIR-778899"),
        ("PairingSecret", "pair-secret-3", "pair-secret-3"),
        ("StreamToken", "stream-tok-4", "stream-tok-4"),
        ("ConnectionString", "Host=pi;Password=db-pass-5;Username=u", "db-pass-5"),
        ("ServiceKey", "0123abcd4567ef89", "0123abcd4567ef89"),
    ];

    /// <summary>Alan adına göre örneği bulur.</summary>
    /// <param name="field">Alan adı.</param>
    /// <returns>Örnek.</returns>
    public static (string Field, string Value, string Secret) Get(string field) => All.Single(s => s.Field == field);
}
