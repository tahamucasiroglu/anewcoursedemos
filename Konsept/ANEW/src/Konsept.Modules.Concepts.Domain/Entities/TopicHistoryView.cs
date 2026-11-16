namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Öneri talimatına giren profil geçmişi (spec 0014 Req 5, G-17): en yeni başlıklar birebir; daha eskileri yalnızca sayı ve en sık
/// kelimelerden oluşan tek kısa özet (ek çağrı yok).
/// </summary>
/// <param name="Verbatim">Birebir giren başlıklar (en yeni önce).</param>
/// <param name="OlderCount">Birebir girmeyen eski başlık sayısı.</param>
/// <param name="OlderKeywords">Eski başlıkların en sık anlamlı kelimeleri.</param>
public sealed record TopicHistoryView(IReadOnlyList<HistoryTitle> Verbatim, int OlderCount, IReadOnlyList<string> OlderKeywords);
