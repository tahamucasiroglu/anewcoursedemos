using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>
/// Duyuru formunun sınıf seçenekleri (spec 0011 gereksinim 1, review M-1): yayının kabul ettiği sınıflar — kurucuda okulun
/// güncel dönemdeki etkin sınıfları, öğretmende iletişim kapısının "kendi sınıfları" (yayın denetimiyle aynı kural; okul
/// günü ve dönem bitişinden bağımsız). Veli ve muhasebe 403.
/// </summary>
public sealed record ListScopeClassroomsQuery : IRequest<IReadOnlyList<ScopeClassroom>>, ILoggableRequest;
