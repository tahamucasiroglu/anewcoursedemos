using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Konuşma başlatma (spec 0011 gereksinim 5): öğrenci ve karşı taraf (veli için öğretmen, öğretmen için veli). Aynı öğretmen–veli–öğrenci üçlüsü için var olan konuşma döner. Yalnızca kimlik taşır.</summary>
public sealed record StartConversationCommand(Guid? StudentId, Guid? CounterpartUserId) : IRequest<ConversationRow>, ILoggableRequest;
