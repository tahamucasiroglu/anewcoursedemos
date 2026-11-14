namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajanın kendini tanıttığı bilgiler (kayıt ve "test et" cevabı).
/// </summary>
public interface IAgentInfoProvider
{
    /// <summary>Gets bilgisayar adı.</summary>
    string MachineName { get; }

    /// <summary>Gets ajan (ürün) versiyonu; sabit metin değil, derleme bilgisinden.</summary>
    string Version { get; }
}
