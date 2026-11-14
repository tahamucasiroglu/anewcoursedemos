namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// İşlenmiş prompt çifti: sistem (sabit, önbelleğe uygun) ve kullanıcı (değişken) metni.
/// </summary>
/// <param name="System">Sistem promptu.</param>
/// <param name="User">Kullanıcı promptu.</param>
public sealed record PromptPair(string System, string User);
