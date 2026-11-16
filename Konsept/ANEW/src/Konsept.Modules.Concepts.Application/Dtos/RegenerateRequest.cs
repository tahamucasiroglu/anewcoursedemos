namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>
/// Talimatla yeniden üretim (spec 0015 Req 7–8): düzenleme talimatı ve — kaydedilmemiş elle değişiklik varsa — düzenleme alanındaki
/// sahneler. Sahne yoksa taban hazır revizyon (Tekrar düzenle), o da yoksa kayıtlı konsepttir.
/// </summary>
/// <param name="Instruction">Düzenleme talimatı.</param>
/// <param name="Scenes">Kaydedilmemiş sahneler.</param>
public sealed record RegenerateRequest(string? Instruction, IReadOnlyList<SceneInput>? Scenes);
