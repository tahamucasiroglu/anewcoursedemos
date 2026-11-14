namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Sıra öğesini yukarı/aşağı taşıma isteği (<c>POST api/production/queue/{id}/move</c>).
/// </summary>
public sealed record MoveQueueItemRequest
{
    /// <summary>Yön: -1 yukarı, +1 aşağı.</summary>
    public int Direction { get; init; }
}
