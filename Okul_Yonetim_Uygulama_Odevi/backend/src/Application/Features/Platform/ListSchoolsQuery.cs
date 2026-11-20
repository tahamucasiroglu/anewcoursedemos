using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Platform;

/// <summary>Okul listesi (platform yöneticisi).</summary>
public sealed record ListSchoolsQuery(string? Search, string? Status, int Page) : IRequest<SchoolPage>;
