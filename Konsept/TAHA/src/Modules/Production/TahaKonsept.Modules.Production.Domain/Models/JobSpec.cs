using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Domain.Models;

/// <summary>
/// Yeni <c>Job</c> oluşturmak için gereken değerler (<c>Job.Create</c>): kullanıcı, sıra öğesi, konsept, başlık,
/// sürüm bilgisi, çözünürlük, ayar/konsept anlık görüntüleri ve (paylaşılacaksa) ajan çalışma alanı.
/// </summary>
public sealed record JobSpec
{
    /// <summary>Kullanıcı.</summary>
    public Guid UserId { get; init; }

    /// <summary>Sıra öğesi.</summary>
    public Guid QueueItemId { get; init; }

    /// <summary>Konsept.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Profil.</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Kaynak iş (yeni sürüm).</summary>
    public Guid? BaseJobId { get; init; }

    /// <summary>Kaynak video.</summary>
    public Guid? ParentVideoId { get; init; }

    /// <summary>Sürüm.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Biçim.</summary>
    public VideoFormat Format { get; init; }

    /// <summary>Genişlik.</summary>
    public int Width { get; init; }

    /// <summary>Yükseklik.</summary>
    public int Height { get; init; }

    /// <summary>Kare hızı.</summary>
    public int Fps { get; init; } = 30;

    /// <summary>Ayar JSON'u.</summary>
    public string SettingsJson { get; init; } = "{}";

    /// <summary>Konsept JSON'u.</summary>
    public string DocumentJson { get; init; } = "{}";

    /// <summary>Paylaşılan ajan çalışma alanı (<c>null</c> = işin kendi kimliği).</summary>
    public Guid? WorkspaceId { get; init; }
}
