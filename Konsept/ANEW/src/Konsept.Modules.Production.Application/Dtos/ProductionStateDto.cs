namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>
/// Konseptten Video sayfasının profil durumu (spec 0006): ayarlar, model seçenekleri (yüklenemediyse <see cref="ModelsFailed"/>), sıra,
/// "Sırayla üret" pasiflik nedeni (kod + metin) ve dil uyarıları. Her işlemden sonra bütünüyle döner.
/// </summary>
/// <param name="ProfileId">Profil.</param>
/// <param name="Settings">Ayarlar.</param>
/// <param name="Models">Seçenekler (katalog sırasıyla).</param>
/// <param name="ModelsFailed">Model listesi yüklenemedi.</param>
/// <param name="Jobs">Sıra (Tamamlandı hariç, sıra numarasıyla).</param>
/// <param name="StartBlock">Pasiflik kodu ya da <c>null</c>.</param>
/// <param name="StartBlockMessage">Pasiflik metni ya da <c>null</c>.</param>
/// <param name="LanguageWarnings">Dil uyarısı metinleri.</param>
/// <param name="RunningHere">Bu profilde çalışan iş var.</param>
/// <param name="AgentConnected">Kullanıcının ajanı bağlı (spec 0017 Req 15: Ajan bekleniyor işi varken "Ajan bağlı değil").</param>
/// <param name="QueueHalted">Art arda 3 iş hata verdi, sıra durdu (spec 0017 Req 11).</param>
public sealed record ProductionStateDto(
    Guid ProfileId,
    ProductionSettingsDto Settings,
    IReadOnlyList<ProductionModelDto> Models,
    bool ModelsFailed,
    IReadOnlyList<ProductionJobDto> Jobs,
    string? StartBlock,
    string? StartBlockMessage,
    IReadOnlyList<string> LanguageWarnings,
    bool RunningHere,
    bool AgentConnected,
    bool QueueHalted);
