using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan konu üreticisi; davranış <see cref="TopicGeneratorBase"/>'tedir.
/// </summary>
/// <param name="gateway">LLM geçidi.</param>
/// <param name="prompts">Prompt fabrikası.</param>
/// <param name="memory">Konu hafızası.</param>
/// <param name="normalizer">Normalleştirici.</param>
public sealed class TopicGenerator(IProviderGateway gateway, IConceptPromptFactory prompts, ITopicMemory memory, ITopicNormalizer normalizer)
    : TopicGeneratorBase(gateway, prompts, memory, normalizer);
