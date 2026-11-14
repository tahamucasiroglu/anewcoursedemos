using System.Text.RegularExpressions;

namespace TahaKonsept.Agent.Engines.Video.Safety;

/// <summary>Sahne kodu denetim kuralı: eşleşirse ihlal sayılan desen ve Türkçe açıklaması.</summary>
/// <param name="Pattern">Yasak desen.</param>
/// <param name="Message">İhlal açıklaması (LLM'e geri bildirim).</param>
public sealed record SceneCodeRule(Regex Pattern, string Message);
