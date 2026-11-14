using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Varsayılan konsept okuyucu (Production için <see cref="IConceptReader"/> ve <see cref="IConceptPronunciationSource"/>);
/// davranış <see cref="ConceptReaderBase"/>'tedir.
/// </summary>
/// <param name="repository">Konsept deposu.</param>
public sealed class ConceptReader(IConceptRepository repository) : ConceptReaderBase(repository);
