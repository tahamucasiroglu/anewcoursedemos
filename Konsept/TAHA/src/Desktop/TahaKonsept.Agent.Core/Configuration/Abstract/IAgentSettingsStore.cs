namespace TahaKonsept.Agent.Core.Configuration.Abstract;

/// <summary>
/// Ajan ayarlarını (<see cref="AgentSettings"/>) okur ve kalıcı olarak yazar. İlk okumada eksik değerler (akış anahtarı,
/// kurulum kimliği, sunucu adresi) üretilip kaydedilir. Değişiklikler <see cref="Changed"/> ile duyurulur.
/// </summary>
public interface IAgentSettingsStore
{
    /// <summary>Güncel ayarlar.</summary>
    AgentSettings Current { get; }

    /// <summary>Ayarları değiştirip kaydeder.</summary>
    /// <param name="change">Eski ayardan yenisini üreten fonksiyon.</param>
    /// <returns>Kaydedilen yeni ayarlar.</returns>
    AgentSettings Update(Func<AgentSettings, AgentSettings> change);

    /// <summary>Ayarlar değişince tetiklenir.</summary>
    event EventHandler<AgentSettings>? Changed;
}
