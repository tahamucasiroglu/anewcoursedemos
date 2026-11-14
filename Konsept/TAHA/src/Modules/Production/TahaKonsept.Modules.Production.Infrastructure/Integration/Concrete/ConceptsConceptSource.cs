using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Infrastructure.Integration.Concrete;

/// <summary>
/// Modüller arası adaptör: Production'ın <c>IConceptSource</c> portunu Concepts modülünün PUBLIC <see cref="IConceptReader"/>'ı üzerinden
/// karşılar (yalnızca Concepts.Application referansı). Kimlik, başlık, profil ve belgeyi <see cref="ConceptSnapshot"/>'a çevirir;
/// silinmiş (<see cref="ConceptDetailDto.DeletedAt"/> dolu ya da durum <see cref="ConceptStatus.Deleted"/>) konseptler için <c>null</c> döner.
/// Başlık boşsa belgedeki başlık kullanılır. Kapsamlı (scoped) kaydedilir; <c>UnavailableConceptSource</c> yedeğinin yerini alır.
/// </summary>
public sealed class ConceptsConceptSource : ConceptSourceBase
{
    private readonly IConceptReader _reader;

    /// <summary>Oluşturur.</summary>
    /// <param name="reader">Concepts okuyucusu.</param>
    public ConceptsConceptSource(IConceptReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _reader = reader;
    }

    /// <summary>Konsepti Concepts modülünden okur ve anlık görüntüye çevirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Anlık görüntü ya da <c>null</c> (yok/silinmiş).</returns>
    protected override async Task<ConceptSnapshot?> ReadAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken)
    {
        var detail = await _reader.GetAsync(userId, conceptId, cancellationToken);
        if (detail is null || detail.DeletedAt is not null || detail.Status == ConceptStatus.Deleted)
        {
            return null;
        }

        var document = detail.Document;
        return new ConceptSnapshot
        {
            ConceptId = detail.Id,
            Title = string.IsNullOrWhiteSpace(detail.Title) ? document.Title : detail.Title,
            ProfileId = detail.ProfileId,
            Document = document,
        };
    }
}
