using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Abstract;

/// <summary>
/// Yerleşik okunuş sözlüğünü sağlar (yaygın yabancı marka/teknoloji/kişi adlarının Türkçe okunuşu: Google → Gugıl …).
/// Kullanıcı ve konsept girdilerinden SONRA uygulanır. Önemli üye: <see cref="Entries"/>.
/// </summary>
public interface IBuiltInLexiconProvider
{
    /// <summary>Yerleşik girdiler (değişmez, önbellekli).</summary>
    IReadOnlyList<PronunciationEntry> Entries { get; }
}
