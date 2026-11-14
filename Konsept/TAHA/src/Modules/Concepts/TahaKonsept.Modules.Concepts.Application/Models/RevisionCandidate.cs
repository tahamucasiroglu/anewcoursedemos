using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Bellekte tutulan düzeltme adayı (kabul edilene ya da atılana kadar, en fazla birkaç saat).
/// </summary>
/// <param name="CandidateId">Aday kimliği.</param>
/// <param name="ConceptId">Konsept.</param>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Instruction">Talimat.</param>
/// <param name="Document">Belge.</param>
/// <param name="InputTokens">Giriş token'ı.</param>
/// <param name="OutputTokens">Çıkış token'ı.</param>
/// <param name="CostUsd">Maliyet.</param>
/// <param name="CreatedAtUtc">Oluşturulma.</param>
public sealed record RevisionCandidate(
    Guid CandidateId,
    Guid ConceptId,
    Guid UserId,
    string Instruction,
    ConceptDocument Document,
    long InputTokens,
    long OutputTokens,
    decimal CostUsd,
    DateTimeOffset CreatedAtUtc);
