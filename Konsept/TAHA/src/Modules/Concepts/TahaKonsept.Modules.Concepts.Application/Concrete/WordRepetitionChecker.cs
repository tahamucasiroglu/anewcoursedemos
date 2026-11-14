using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan kelime tekrar denetimi; davranış <see cref="WordRepetitionCheckerBase"/>'tedir.
/// </summary>
/// <param name="analyzer">Yerel analizci.</param>
/// <param name="gateway">LLM geçidi.</param>
/// <param name="prompts">Prompt fabrikası.</param>
public sealed class WordRepetitionChecker(IWordRepetitionAnalyzer analyzer, IProviderGateway gateway, IConceptPromptFactory prompts)
    : WordRepetitionCheckerBase(analyzer, gateway, prompts);
