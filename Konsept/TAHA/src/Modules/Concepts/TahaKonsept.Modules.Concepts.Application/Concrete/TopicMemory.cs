using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan konu hafızası; davranış <see cref="TopicMemoryBase"/>'tedir.
/// </summary>
/// <param name="repository">Hafıza deposu.</param>
/// <param name="normalizer">Normalleştirici.</param>
public sealed class TopicMemory(ITopicMemoryRepository repository, ITopicNormalizer normalizer) : TopicMemoryBase(repository, normalizer);
