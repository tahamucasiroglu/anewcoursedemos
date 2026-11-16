namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajan oturum belirtecinin ajandaki yeri (spec 0012 Req 9): eşleşmiş ajan yeniden başlatılınca yeniden eşleştirme istemez.
/// Belirteç hiçbir loga yazılmaz.
/// </summary>
public interface IAgentTokenStore
{
    /// <summary>Saklı belirteci okur.</summary>
    /// <returns>Belirteç; eşleşmemişse <c>null</c>.</returns>
    string? Read();

    /// <summary>Belirteci saklar.</summary>
    /// <param name="token">Belirteç.</param>
    void Save(string token);

    /// <summary>Belirteci siler (oturum kapandı).</summary>
    void Clear();
}
