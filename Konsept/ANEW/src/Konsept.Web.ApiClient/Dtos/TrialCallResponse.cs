namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Deneme çağrısının sonucu (<c>POST api/ai/calls/trial</c>, plan 0013).</summary>
/// <param name="ModelId">Model.</param>
/// <param name="Text">Yanıt metni (kısaltılmış).</param>
/// <param name="Truncated">Yarım kaldı mı.</param>
/// <param name="InputTokens">Giriş token.</param>
/// <param name="OutputTokens">Çıkış token.</param>
/// <param name="CostUsd">Maliyet (USD).</param>
/// <param name="IsEstimated">Tahmini mi.</param>
/// <param name="Attempts">Deneme sayısı.</param>
public sealed record TrialCallResponse(
    string ModelId,
    string Text,
    bool Truncated,
    int? InputTokens,
    int? OutputTokens,
    decimal CostUsd,
    bool IsEstimated,
    int Attempts);
