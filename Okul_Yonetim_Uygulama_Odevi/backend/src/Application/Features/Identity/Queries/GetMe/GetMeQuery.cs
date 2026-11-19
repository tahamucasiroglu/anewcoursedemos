using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Queries.GetMe;

/// <summary>Hoş geldin ekranının bilgisi: kişi, seçili okul, o okuldaki rolleri, görebildiği şubeler ve tüm okulları.</summary>
public sealed record GetMeQuery : IRequest<MeResponse>, ILoggableRequest;
