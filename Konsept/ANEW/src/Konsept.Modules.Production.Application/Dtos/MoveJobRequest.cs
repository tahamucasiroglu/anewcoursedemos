namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Yukarı/Aşağı (spec 0006 Req 11).</summary>
public sealed record MoveJobRequest
{
    /// <summary>Yukarı.</summary>
    public const string Up = "up";

    /// <summary>Aşağı.</summary>
    public const string Down = "down";

    /// <summary>Gets yön (<see cref="Up"/> · <see cref="Down"/>).</summary>
    public string? Direction { get; init; }
}
