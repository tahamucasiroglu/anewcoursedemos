namespace Konsept.Modules.AiGateway.Contracts.Dtos;

/// <summary>
/// Model çağrısı hatalarının başka modüllerin karar verdiği kararlı kodları (spec 0017 Req 16): ör. key yoksa iş "API key yok — Ayarlar"
/// açıklamasıyla durur.
/// </summary>
public static class ModelCallErrorCodes
{
    /// <summary>Çağrı için key girilmemiş (sağlayıcıya gidilmedi, harcama yok).</summary>
    public const string NoKey = "ai.no_key";
}
