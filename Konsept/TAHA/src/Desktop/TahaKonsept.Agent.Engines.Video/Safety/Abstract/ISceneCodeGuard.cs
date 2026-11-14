using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Safety.Abstract;

/// <summary>
/// LLM'in yazdığı sahne kodunu render öncesi statik olarak denetler: ağ erişimi (fetch, XMLHttpRequest, WebSocket,
/// http/https adresleri, dinamik import), <c>file://</c>, rastgelelik/saat gibi deterministik olmayan API'ler.
/// Önemli üye: <see cref="Check"/>.
/// </summary>
public interface ISceneCodeGuard
{
    /// <summary>Kodu denetler; ihlal varsa hepsini listeleyen doğrulama hatası döner.</summary>
    /// <param name="sceneCode">Sahne kodu.</param>
    /// <returns>Başarı ya da ayrıntılı hata (LLM'e geri bildirim için).</returns>
    Result Check(string sceneCode);
}
