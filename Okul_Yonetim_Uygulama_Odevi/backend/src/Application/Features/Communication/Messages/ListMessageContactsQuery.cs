using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>"Yeni mesaj" seçenekleri: veli için çocukları ve sınıflarının öğretmenleri, öğretmen için sınıfları → öğrenciler → veliler.</summary>
public sealed record ListMessageContactsQuery : IRequest<MessageContacts>, ILoggableRequest;
