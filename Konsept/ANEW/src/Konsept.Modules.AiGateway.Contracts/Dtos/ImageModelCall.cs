namespace Konsept.Modules.AiGateway.Contracts.Dtos;

/// <summary>Online görsel modeli çağrısı (spec 0017 Req 13): çağıran, model, profil ve görsel tarifi; tek görsel üretilir.</summary>
public sealed record ImageModelCall
{
    /// <summary>Gets çağıran.</summary>
    public required ModelCallerIdentity Caller { get; init; }

    /// <summary>Gets katalog model kimliği.</summary>
    public required string ModelId { get; init; }

    /// <summary>Gets ilgili profil.</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Gets görsel tarifi.</summary>
    public required string Prompt { get; init; }
}
