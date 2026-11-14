using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Konsept üretim / düzeltme çağrısının sonucu: belge, model, token kullanımı, maliyet ve süre.
/// </summary>
/// <param name="Document">Belge.</param>
/// <param name="ModelKey">Model anahtarı.</param>
/// <param name="InputTokens">Giriş token'ı (önbellek dahil).</param>
/// <param name="OutputTokens">Çıkış token'ı.</param>
/// <param name="CostUsd">Maliyet.</param>
/// <param name="DurationMs">Süre (ms).</param>
public sealed record GeneratedConcept(
    ConceptDocument Document,
    string ModelKey,
    long InputTokens,
    long OutputTokens,
    decimal CostUsd,
    long DurationMs);
