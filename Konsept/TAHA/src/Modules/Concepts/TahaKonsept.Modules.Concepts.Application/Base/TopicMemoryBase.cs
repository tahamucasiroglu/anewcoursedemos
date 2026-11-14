using System.Text;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Modules.Concepts.Domain.Enums;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="ITopicMemory"/> ortak davranışı (depo + normalleştirici). Prompt listesi: en yeni önce, en çok <see cref="MaxPromptItems"/>
/// satır ve <see cref="MaxPromptChars"/> karakter; başlıklar <see cref="MaxTitleChars"/>'ta kısaltılır; negatifler "✗", diğerleri "•".
/// </summary>
public abstract class TopicMemoryBase : ITopicMemory
{
    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Hafıza deposu.</param>
    /// <param name="normalizer">Normalleştirici.</param>
    protected TopicMemoryBase(ITopicMemoryRepository repository, ITopicNormalizer normalizer)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
    }

    /// <summary>Hafıza deposu.</summary>
    protected ITopicMemoryRepository Repository { get; }

    /// <summary>Normalleştirici.</summary>
    protected ITopicNormalizer Normalizer { get; }

    /// <summary>Prompttaki en fazla satır.</summary>
    protected virtual int MaxPromptItems => 120;

    /// <summary>Prompttaki en fazla karakter.</summary>
    protected virtual int MaxPromptChars => 3500;

    /// <summary>Satırdaki en fazla başlık uzunluğu.</summary>
    protected virtual int MaxTitleChars => 70;

    /// <summary>Profil hafızasını yükler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Anlık görüntü.</returns>
    public virtual async Task<TopicMemorySnapshot> LoadAsync(Guid userId, Guid? profileId, CancellationToken cancellationToken = default)
    {
        var entries = await Repository.ListForProfileAsync(userId, profileId, cancellationToken);
        return new TopicMemorySnapshot(entries
            .Select(e => new TopicMemoryItem(e.Title, e.Kind, Normalizer.FromStored(e.Keywords, e.Hash), e.UpdatedAtUtc))
            .ToList());
    }

    /// <summary>Başlıkları kaydeder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="titles">Başlıklar.</param>
    /// <param name="kind">Tür.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual async Task RememberTitlesAsync(Guid userId, Guid? profileId, IEnumerable<string> titles, TopicMemoryKind kind, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(titles);
        var list = titles.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (list.Count == 0)
        {
            return;
        }

        var entries = (await Repository.ListForProfileTrackedAsync(userId, profileId, cancellationToken)).ToList();
        foreach (var title in list)
        {
            var fp = Normalizer.Fingerprint(title);
            if (fp.Words.Count == 0)
            {
                continue;
            }

            var existing = entries.FirstOrDefault(e => Normalizer.IsDuplicate(Normalizer.FromStored(e.Keywords, e.Hash), fp));
            if (existing is not null)
            {
                existing.Promote(kind);
                continue;
            }

            var entry = TopicMemoryEntry.Create(userId, profileId, title, fp.Keywords, fp.Hash, kind);
            await Repository.AddAsync(entry, cancellationToken);
            entries.Add(entry);
        }

        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Konseptin hafıza kaydını yazar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="title">Başlık.</param>
    /// <param name="kind">Tür.</param>
    /// <param name="forceKind">Zayıflatmaya izin.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual async Task MarkConceptAsync(Guid userId, Guid? profileId, Guid conceptId, string title, TopicMemoryKind kind, bool forceKind = false, CancellationToken cancellationToken = default)
    {
        var linked = await Repository.ListByConceptTrackedAsync(userId, conceptId, cancellationToken);
        if (linked.Count > 0)
        {
            foreach (var entry in linked)
            {
                Apply(entry, kind, conceptId, forceKind);
            }
        }
        else
        {
            var fp = Normalizer.Fingerprint(title ?? string.Empty);
            if (fp.Words.Count == 0)
            {
                return;
            }

            var entries = await Repository.ListForProfileTrackedAsync(userId, profileId, cancellationToken);
            var existing = entries.FirstOrDefault(e => Normalizer.IsDuplicate(Normalizer.FromStored(e.Keywords, e.Hash), fp));
            if (existing is not null)
            {
                Apply(existing, kind, conceptId, forceKind);
            }
            else
            {
                await Repository.AddAsync(TopicMemoryEntry.Create(userId, profileId, title!, fp.Keywords, fp.Hash, kind, conceptId), cancellationToken);
            }
        }

        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Konsepte bağlı kayıtları siler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual async Task ForgetConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken = default)
    {
        var linked = await Repository.ListByConceptTrackedAsync(userId, conceptId, cancellationToken);
        if (linked.Count == 0)
        {
            return;
        }

        foreach (var entry in linked)
        {
            Repository.Remove(entry);
        }

        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Hafızayı sıfırlar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="all">Hepsi mi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen sayısı.</returns>
    public virtual async Task<int> ResetAsync(Guid userId, bool all, CancellationToken cancellationToken = default)
    {
        var entries = await Repository.ListForUserTrackedAsync(userId, cancellationToken);
        var targets = all ? entries : entries.Where(e => e.Kind is TopicMemoryKind.Suggested or TopicMemoryKind.Disliked).ToList();
        foreach (var entry in targets)
        {
            Repository.Remove(entry);
        }

        if (targets.Count > 0)
        {
            await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        return targets.Count;
    }

    /// <summary>Prompt listesini üretir.</summary>
    /// <param name="snapshot">Hafıza.</param>
    /// <returns>Satırlar.</returns>
    public virtual string RenderForPrompt(TopicMemorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var sb = new StringBuilder();
        var count = 0;
        foreach (var item in snapshot.Items)
        {
            if (count >= MaxPromptItems)
            {
                break;
            }

            var title = item.Title.Length <= MaxTitleChars ? item.Title : item.Title[..MaxTitleChars].TrimEnd() + "…";
            var line = (item.IsNegative ? "✗ " : "• ") + title;
            if (sb.Length + line.Length + 1 > MaxPromptChars)
            {
                break;
            }

            sb.Append(line).Append('\n');
            count++;
        }

        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>Başlık biliniyor mu.</summary>
    /// <param name="snapshot">Hafıza.</param>
    /// <param name="fingerprint">Parmak izi.</param>
    /// <returns>Biliniyor mu.</returns>
    public virtual bool IsKnown(TopicMemorySnapshot snapshot, TopicFingerprint fingerprint)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(fingerprint);
        return snapshot.Items.Any(i => Normalizer.IsDuplicate(i.Fingerprint, fingerprint));
    }

    private static void Apply(TopicMemoryEntry entry, TopicMemoryKind kind, Guid conceptId, bool force)
    {
        if (force)
        {
            entry.Demote(kind);
        }

        entry.Promote(kind, conceptId);
    }
}
