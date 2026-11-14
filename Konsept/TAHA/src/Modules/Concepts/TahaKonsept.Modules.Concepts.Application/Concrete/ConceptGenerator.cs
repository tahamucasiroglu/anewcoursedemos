using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan konsept üreticisi; davranış <see cref="ConceptGeneratorBase"/>'tedir.
/// </summary>
/// <param name="gateway">LLM geçidi.</param>
/// <param name="prompts">Prompt fabrikası.</param>
public sealed class ConceptGenerator(IProviderGateway gateway, IConceptPromptFactory prompts) : ConceptGeneratorBase(gateway, prompts);
