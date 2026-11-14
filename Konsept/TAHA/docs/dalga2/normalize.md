# Dalga 2 — normalize (Türkçe okunuş normalizasyonu)

The Turkish speech-text normalizer is in place. It builds with 0 warnings, and all 236 test cases pass. The three pre-created signatures (`ISpeechTextNormalizer`, `NormalizedText`, `NormalizationChange`) are unchanged.

The required sentence comes out as specified: "II. Mehmed 29 Mayıs 1453'te İstanbul'u fethetti." → "İkinci Mehmed yirmi dokuz Mayıs bin dört yüz elli üçte İstanbul'u fethetti."

## Public types
Project: `src/Modules/Production/TahaKonsept.Modules.Production.Normalization`

**Interfaces (`.Abstract`)**
- `ISpeechTextNormalizer` (unchanged).
- `ILanguageSpeechTextNormalizer : ISpeechTextNormalizer` adds `string Name` and `bool Supports(string language)`.
- `INormalizerSelector` has `ILanguageSpeechTextNormalizer Select(string language)`.
- `INormalizationRule` has `string Key` and `void Apply(NormalizationContext context)`.
- `INumberToTurkishWords` has `ToWords(long)`, `ToWords(string digits)`, `ToDecimalWords(int, frac, separatorWord)`, `ToOrdinal(long)`, `ToOrdinalWords(string)` and `Vocabulary`.
- `ITurkishSuffixHarmonizer` has `HarmonizeSuffix(stem, suffix)` and `Attach(stem, suffix)`.
- `IBuiltInLexiconProvider` has `IReadOnlyList<PronunciationEntry> Entries`.

**Base classes (`.Base`)**
- `SpeechTextNormalizerBase`: an empty language is treated as "tr".
- `RuleChainSpeechTextNormalizerBase`, `NormalizationRuleBase`, `NormalizerSelectorBase`, `NumberToTurkishWordsBase`, `TurkishSuffixHarmonizerBase`, `BuiltInLexiconProviderBase`.

**Normalizers and helpers (`.Concrete`)**
- `TurkishSpeechTextNormalizer` handles "tr", "tr-TR" and "tr_TR".
- `LexiconOnlySpeechTextNormalizer` is used for every other language. It applies only the user/concept lexicon and leaves suffixes as written.
- `LanguageRoutingSpeechTextNormalizer` picks one of the two through the selector `LanguageNormalizerSelector`.
- `TurkishNumberToWords`, `TurkishSuffixHarmonizer` and `EmbeddedBuiltInLexiconProvider`.

**Rule chain (`.Concrete.Rules`)**, in order: `LexiconRule`, `DateRule`, `RomanNumeralRule`, `OrdinalRule`, `NumberRule`, `AbbreviationRule`, `SymbolRule`.

**Other pieces**
- **`.Models`:** `NormalizationContext` and `NormalizationRuleKeys`. Every change is recorded in `Changes`, and `Display` is never modified.
- **`.Text` (static helpers):** `NumberToTurkishWords` (e.g. `NumberToTurkishWords.ToWords(1453)`), `TurkishCulture`, `RomanNumerals`, `TurkishLetterNames`, `KnownAcronyms`, `MeasurementUnits`, `DottedAbbreviations`, `TurkishMonths`.
- **DI:** `services.AddSpeechTextNormalization()` in `.DependencyInjection`; all singletons; `ISpeechTextNormalizer` → `LanguageRoutingSpeechTextNormalizer`.
- **Without DI:** `SpeechTextNormalizerFactory.CreateDefault()`, `CreateTurkish()`, `CreateSelector()` in `.Factory`.
- **Built-in lexicon:** `Resources/builtin-lexicon-tr.json` (~180 entries). Named `-tr.json` because MSBuild treats `.tr.` as a culture and moves it into a satellite assembly.

## Choices you may want to change
- **Turkish casing:** I/İ/ı/i mapped by hand on top of tr-TR (correct without ICU). For lexicon matching ı and i are the same letter.
- **Suffixes re-harmonized:** "1923'de" → "üçte"; "kg'ye" → "kilograma"; "4'ü" → "dördü".
- **Apostrophe:** numbers/units join without apostrophe ("üçte"); lexicon words and acronyms keep it ("Gugıl'ın", "a be de'de").
- **Ordinals before a capital:** "3. Selim" → "Üçüncü Selim" only up to 50; above that the dot is a sentence end.
- **Single-letter Roman numerals:** only I, V, X with a dot and a following word ("M. Kemal" unchanged).
- **All-caps words:** 4+ pronounceable letters read as words ("DİKKAT" → "Dikkat"); others spelled with Turkish letter names; known list (NATO, TÜBİTAK, "VE", "BU").

## Tests and commits
- `tests/TahaKonsept.Modules.Production.Normalization.Tests`: 236 table-driven cases. No database.
- Commits: `0df8d33`, `b9ab4d1`. Not pushed.

## Not done / for the integrator
- Library only (no routes/pages).
- Production calls `AddSpeechTextNormalization()` in `AddProductionCore` and passes the approved dictionary + concept `pronunciations` (*entegratör: doğrulandı*).
- Known small gaps: currency symbol separated from its number ("39 milyar $") gets its suffix unharmonized; "COVID-19" → "Kovid-on dokuz"; number lists like "1,2,3" only partly converted.
