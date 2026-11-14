using TahaKonsept.Modules.Concepts.Application.Base;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Şablonları derlemeye gömülü <c>Prompts/&lt;ad&gt;.md</c> kaynaklarından okuyan kütüphane.
/// </summary>
public sealed class EmbeddedPromptLibrary : PromptLibraryBase
{
    /// <summary>Kaynak adı öneki.</summary>
    public const string ResourcePrefix = "TahaKonsept.Modules.Concepts.Application.Prompts.";

    /// <summary>Gömülü kaynağı okur.</summary>
    /// <param name="name">Ad.</param>
    /// <returns>Şablon.</returns>
    protected override string Load(string name)
    {
        var resource = ResourcePrefix + name + ".md";
        using var stream = typeof(EmbeddedPromptLibrary).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Gömülü prompt bulunamadı: {resource}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
