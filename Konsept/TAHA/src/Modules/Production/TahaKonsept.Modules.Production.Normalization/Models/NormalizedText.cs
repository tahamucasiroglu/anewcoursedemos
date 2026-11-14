namespace TahaKonsept.Modules.Production.Normalization.Models;

/// <summary>Normalizasyon sonucu: <see cref="Display"/> (yazılış, altyazı), <see cref="Spoken"/> (TTS'e giden okunuş) ve
/// <see cref="Changes"/> (önizleme ekranında gösterilen değişiklikler).</summary>
/// <param name="Display">Yazılış metni (değişmez).</param>
/// <param name="Spoken">Okunuş metni.</param>
/// <param name="Changes">Yapılan değişiklikler.</param>
public sealed record NormalizedText(string Display, string Spoken, IReadOnlyList<NormalizationChange> Changes);
