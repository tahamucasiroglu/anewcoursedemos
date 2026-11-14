namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Prompt şablon kütüphanesi (gömülü <c>Prompts/*.md</c>; bir kez okunur, bellekte tutulur).
/// </summary>
public interface IPromptLibrary
{
    /// <summary>Şablonu getirir.</summary>
    /// <param name="name">Ad (bkz. <c>PromptNames</c>).</param>
    /// <returns>Şablon metni.</returns>
    string Get(string name);
}
