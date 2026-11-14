using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan prompt fabrikası; davranış <see cref="ConceptPromptFactoryBase"/>'tedir.
/// </summary>
/// <param name="library">Şablon kütüphanesi.</param>
/// <param name="renderer">Şablon işleyici.</param>
public sealed class ConceptPromptFactory(IPromptLibrary library, IPromptTemplateRenderer renderer)
    : ConceptPromptFactoryBase(library, renderer);
