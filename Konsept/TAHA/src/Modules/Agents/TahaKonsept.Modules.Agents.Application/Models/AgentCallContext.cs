namespace TahaKonsept.Modules.Agents.Application.Models;

/// <summary>
/// Hub çağrısını yapan ajanın bağlamı: SignalR bağlantı kimliği, kullanıcı ve (biliniyorsa) eşlenmiş cihaz.
/// <c>IAgentStageEventHandler</c> ve <c>IAgentLogSink</c> uygulamalarına iletilir.
/// </summary>
/// <param name="ConnectionId">SignalR bağlantı kimliği.</param>
/// <param name="UserId">Kullanıcı kimliği.</param>
/// <param name="DeviceId">Cihaz kimliği (token claim'inden ya da Hello'dan; bilinmiyorsa <c>null</c>).</param>
public sealed record AgentCallContext(string ConnectionId, Guid UserId, Guid? DeviceId);
