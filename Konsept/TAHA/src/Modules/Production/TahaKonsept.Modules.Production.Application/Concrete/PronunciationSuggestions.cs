using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan okunuş önerisi ekleyici (scoped).
/// </summary>
/// <param name="repository">Sözlük deposu.</param>
public sealed class PronunciationSuggestions(IPronunciationRepository repository) : PronunciationSuggestionsBase(repository);
