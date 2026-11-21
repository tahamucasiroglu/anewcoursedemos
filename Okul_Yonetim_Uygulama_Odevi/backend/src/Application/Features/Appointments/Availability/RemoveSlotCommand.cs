using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>Öğretmenin boş ve başlamamış dilimini kaldırması (spec 0016 C-4); talebi olan dilim kaldırılamaz. Sonuç kaldırılan dilimin kimliği.</summary>
public sealed record RemoveSlotCommand(Guid SlotId) : IRequest<Guid>;
