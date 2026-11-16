namespace Konsept.Agent.Application.Base;

/// <summary>Oturum akışının zamanlamaları.</summary>
/// <param name="PollInterval">Kod durumu sorgu aralığı.</param>
/// <param name="RetryDelay">Sunucuya ulaşılamadığında yeniden deneme aralığı (0001'deki yeniden bağlanma aralığı).</param>
public sealed record AgentSessionOptions(TimeSpan PollInterval, TimeSpan RetryDelay);
