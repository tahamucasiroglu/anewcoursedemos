using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Aşama işleyicilerine verilen bağlam (bir iş için): kullanıcı, iş/video/çalışma alanı kimlikleri, başlık, çözünürlük,
/// ayar ve konsept anlık görüntüleri, okunuş sözlüğü (onaylı kullanıcı kayıtları + konsept önerileri) ve üst aşama çıktıları (<see cref="Outputs"/>).
/// Yardımcılar: <see cref="SceneCount"/>, <see cref="Language"/>, <see cref="NormalizeEnabled"/>.
/// </summary>
public sealed record PipelineContext
{
    /// <summary>Kullanıcı.</summary>
    public Guid UserId { get; init; }

    /// <summary>İş.</summary>
    public Guid JobId { get; init; }

    /// <summary>Oluşacak video.</summary>
    public Guid VideoId { get; init; }

    /// <summary>Ajan çalışma alanı (komutlarda <c>JobId</c> olarak gider).</summary>
    public Guid WorkspaceId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Genişlik.</summary>
    public int Width { get; init; }

    /// <summary>Yükseklik.</summary>
    public int Height { get; init; }

    /// <summary>Kare hızı.</summary>
    public int Fps { get; init; }

    /// <summary>Ayarlar.</summary>
    public ProductionSettingsDto Settings { get; init; } = new();

    /// <summary>Konsept.</summary>
    public ConceptDocument Document { get; init; } = new();

    /// <summary>Okunuş sözlüğü (öncelik sırasıyla).</summary>
    public IReadOnlyList<PronunciationEntry> Lexicon { get; init; } = [];

    /// <summary>Üst aşama çıktıları.</summary>
    public required IStageOutputView Outputs { get; init; }

    /// <summary>Sahne sayısı.</summary>
    public int SceneCount => Document.Scenes.Count;

    /// <summary>Dil kodu.</summary>
    public string Language => string.IsNullOrWhiteSpace(Document.Language) ? "tr" : Document.Language.Trim();

    /// <summary>Seçili ses modeli için okunuş düzeltmesi açık mı (listede yoksa açık).</summary>
    public bool NormalizeEnabled =>
        Settings.SpeechModelKey is null
        || !Settings.NormalizeBySpeechModel.TryGetValue(Settings.SpeechModelKey, out var enabled)
        || enabled;
}
