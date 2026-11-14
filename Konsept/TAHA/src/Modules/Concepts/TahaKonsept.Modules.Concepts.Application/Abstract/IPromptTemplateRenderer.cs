namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Küçük şablon işleyici: <c>{{ad}}</c> değişkenleri ve <c>{{#ad}}…{{/ad}}</c> koşullu bölümleri (değer boşsa bölüm atlanır);
/// fazla boş satırları sıkıştırır.
/// </summary>
public interface IPromptTemplateRenderer
{
    /// <summary>Şablonu işler.</summary>
    /// <param name="template">Şablon.</param>
    /// <param name="values">Değerler.</param>
    /// <returns>Metin.</returns>
    string Render(string template, IReadOnlyDictionary<string, string?> values);
}
