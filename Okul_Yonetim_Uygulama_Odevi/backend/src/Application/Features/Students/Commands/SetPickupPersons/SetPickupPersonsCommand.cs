using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Commands.SetPickupPersons;

/// <summary>
/// Teslim alabilecek kişiler listesini kaydet (spec 0009 gereksinim 10): verilen liste kartın yeni listesi olur (ekle,
/// düzenle, kaldır tek kayıtta); en fazla 10 kişi. Kişisel veri taşır; <c>ILoggableRequest</c> değildir.
/// </summary>
public sealed record SetPickupPersonsCommand(Guid StudentId, IReadOnlyList<PickupPersonInput>? Persons) : IRequest<StudentSaved>;
