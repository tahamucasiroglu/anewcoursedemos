namespace Konsept.Modules.AgentGateway.Contracts;

/// <summary>
/// API ile ajan arasındaki SignalR sözleşmesi: hub yolu ve metot adları. İki taraf da bu sabitleri kullanır.
/// </summary>
public static class AgentHubContract
{
    /// <summary>Hub'ın API üzerindeki yolu.</summary>
    public const string HubPath = "/hubs/agent";

    /// <summary>Ajan → API: bağlanınca (ve her yeniden bağlanmada) kendini kaydeder.</summary>
    public const string Register = "Register";

    /// <summary>API → ajan: "Ajanı test et" komutu; ajan sürümünü ve makine adını döndürür.</summary>
    public const string Ping = "Ping";

    /// <summary>
    /// API → ajan: ajanın oturumu iptal edildi (çıkış ya da yeni eşleştirme, spec 0012 Req 6, 12); ajan "Oturum kapandı" gösterir
    /// ve yeni kod ister. Hemen ardından bağlantı sunucu tarafından kesilir.
    /// </summary>
    public const string SessionClosed = "SessionClosed";

    /// <summary>Ajan → API: Error/Critical log özeti (spec 0008 Req 7); yalnızca kayıtlı bağlantıdan kabul edilir.</summary>
    public const string ReportLog = "ReportLog";

    /// <summary>Ajan → API: kullanıcının seçtiği hata bildirimi (spec 0022 Req 4); yalnızca geçerli ajan oturumundan.</summary>
    public const string ReportError = "ReportError";

    /// <summary>Ajan → API: ajanın eşleştiği hesabın kimliği (spec 0022 Req 8; bekleyen bildirimi hesaba bağlamak için).</summary>
    public const string WhoAmI = "WhoAmI";

    /// <summary>API → ajan: üretim için gereken araçlar kurulu mu (spec 0017 Req 14); eksiklerin kimliklerini döndürür.</summary>
    public const string CheckTools = "CheckTools";

    /// <summary>API → ajan: bir yerel üretim aşamasını çalıştır (görseller, seslendirme, render, birleştirme; spec 0017 Req 13).</summary>
    public const string RunStage = "RunStage";

    /// <summary>API → ajan: işin süren yerel aşamasını kes (Durdur, spec 0017 Req 9).</summary>
    public const string CancelStage = "CancelStage";

    /// <summary>API → ajan: bir video için kısa ömürlü yerel akış adresi aç (spec 0017 Req 21, Ç-1).</summary>
    public const string OpenStream = "OpenStream";

    /// <summary>API → ajan: videonun klasörünü ya da videoyu sistem oynatıcısında aç (spec 0007 Req 4).</summary>
    public const string OpenFile = "OpenFile";

    /// <summary>API → ajan: silinen videonun tüm sürüm dosyalarını kalıcı sil (spec 0019 Req 4).</summary>
    public const string DeleteFiles = "DeleteFiles";
}
