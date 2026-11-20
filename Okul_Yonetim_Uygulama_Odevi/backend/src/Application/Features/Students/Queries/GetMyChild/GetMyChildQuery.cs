using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Queries.GetMyChild;

/// <summary>Velinin bağlı çocuğunun salt okunur kartı; bağ yoksa ya da çocuk ayrıldıysa 404. Her okunuşu audit'e geçer.</summary>
public sealed record GetMyChildQuery(Guid Id) : IRequest<ChildCard>;
