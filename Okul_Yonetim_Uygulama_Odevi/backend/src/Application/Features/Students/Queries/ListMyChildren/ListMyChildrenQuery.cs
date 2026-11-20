using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Queries.ListMyChildren;

/// <summary>Velinin "Çocuklarım" listesi (seçili okulda bağlı ve ayrılmamış çocuklar; spec 0009 gereksinim 11).</summary>
public sealed record ListMyChildrenQuery : IRequest<IReadOnlyList<ChildRow>>;
