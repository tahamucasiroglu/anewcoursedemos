namespace TahaKonsept.Agent.Core.Auth;

/// <summary>
/// <c>POST api/identity/refresh</c> gövdesi (<c>{ refreshToken }</c>). Identity modülü Shared.Contracts'a
/// <c>RefreshRequest</c> eklediğinde aynı JSON biçimiyle uyumludur.
/// </summary>
/// <param name="RefreshToken">Yenileme token'ı.</param>
public sealed record AgentRefreshRequest(string RefreshToken);
