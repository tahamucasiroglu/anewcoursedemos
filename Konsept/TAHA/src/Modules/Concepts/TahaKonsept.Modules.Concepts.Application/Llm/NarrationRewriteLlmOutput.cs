namespace TahaKonsept.Modules.Concepts.Application.Llm;

/// <summary>
/// Kelime tekrar denetimi modelinin JSON çıktısı: yalnızca yeniden yazılan sahnelerin seslendirmeleri.
/// </summary>
public sealed record NarrationRewriteLlmOutput
{
    /// <summary>Yeniden yazılan sahneler.</summary>
    public List<NarrationRewriteLlm> Scenes { get; init; } = [];
}
