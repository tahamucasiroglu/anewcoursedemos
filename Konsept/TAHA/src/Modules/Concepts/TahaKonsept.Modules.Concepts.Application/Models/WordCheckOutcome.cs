using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Kelime tekrar denetimi sonucu: (gerekirse yeniden yazılmış) belge, bulgular, yeniden yazılan sahne sayısı ve LLM kullanımı
/// (bulgu yoksa LLM çağrılmaz, kullanım 0).
/// </summary>
/// <param name="Document">Belge.</param>
/// <param name="Findings">Bulgular.</param>
/// <param name="RewrittenScenes">Yeniden yazılan sahne sayısı.</param>
/// <param name="InputTokens">Giriş token'ı.</param>
/// <param name="OutputTokens">Çıkış token'ı.</param>
/// <param name="CostUsd">Maliyet.</param>
public sealed record WordCheckOutcome(
    ConceptDocument Document,
    IReadOnlyList<WordRepetitionFinding> Findings,
    int RewrittenScenes,
    long InputTokens,
    long OutputTokens,
    decimal CostUsd);
