using System.Collections.Concurrent;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Sahte konsept portu: <see cref="Add"/> ile eklenen konseptleri döner; <see cref="Sample"/> örnek Türkçe konsept üretir.
/// </summary>
public sealed class FakeConceptSource : IConceptSource
{
    private readonly ConcurrentDictionary<Guid, ConceptSnapshot> _items = new();

    /// <summary>Konsept ekler.</summary>
    /// <param name="document">Belge.</param>
    /// <returns>Konsept kimliği.</returns>
    public Guid Add(ConceptDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var id = Guid.CreateVersion7();
        _items[id] = new ConceptSnapshot { ConceptId = id, Title = document.Title, Document = document };
        return id;
    }

    /// <summary>Konsepti değiştirir.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="document">Yeni belge.</param>
    public void Replace(Guid id, ConceptDocument document) =>
        _items[id] = new ConceptSnapshot { ConceptId = id, Title = document.Title, Document = document };

    /// <inheritdoc />
    public Task<ConceptSnapshot?> GetAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        Task.FromResult(_items.TryGetValue(conceptId, out var item) ? item : null);

    /// <summary>Örnek konsept (3 sahne, okunuş önerisi, bir kaynak).</summary>
    /// <param name="title">Başlık.</param>
    /// <returns>Belge.</returns>
    public static ConceptDocument Sample(string title = "İstanbul'un Fethi") => new()
    {
        Title = title,
        Summary = "1453 fethi kısa anlatım",
        Language = "tr",
        Format = VideoFormat.Vertical,
        TargetDurationSec = 30,
        Tone = "heyecanlı",
        Scenes =
        [
            new ConceptScene { Index = 0, Title = "Giriş", OnScreen = "1453", Narration = "29 Mayıs 1453 sabahı şehir sessizdi.", VisualPrompt = "Konstantinopolis surları şafakta" },
            new ConceptScene { Index = 1, Title = "Kuşatma", OnScreen = "53 gün", Narration = "Kuşatma tam elli üç gün sürdü ve Google bile bunu bilir.", VisualPrompt = "Osmanlı topları" },
            new ConceptScene { Index = 2, Title = "Fetih", OnScreen = "Yeni çağ", Narration = "II. Mehmed şehre girdi ve bir çağ kapandı.", VisualPrompt = "Ayasofya önünde atlı padişah" },
        ],
        Sources = [new ConceptSource { Title = "Vikipedi", Url = "https://tr.wikipedia.org/wiki/İstanbul'un_Fethi" }],
        Pronunciations = [new PronunciationEntry { Written = "Google", Spoken = "Gugıl" }],
    };
}
