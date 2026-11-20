using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>
/// Diğer modüllerin (spec 0012: Ai — sesli not) konuşma bağlamı okuması: çağıran konuşmanın tarafı değilse 404 (başkası,
/// kurucu dahil); taraf ise konuşmanın öğrencisi, çağıranın öğretmen tarafı olup olmadığı ve ilişkinin sürüp sürmediği
/// (yeni mesaj gönderilebilir mi). Yalnızca kimlik taşıdığı için loglanabilir.
/// </summary>
public sealed record GetTeacherConversationContextQuery(Guid ConversationId) : IRequest<TeacherConversationContext>, ILoggableRequest;
