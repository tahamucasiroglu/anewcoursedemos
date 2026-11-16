using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Modules.AiGateway.Domain.Rules;

/// <summary>
/// Maliyet hesabı (spec 0013 Req 12–13, E-3): yalnızca <see cref="decimal"/> ile, yuvarlamadan. Metin: giriş token × giriş fiyatı +
/// çıkış token × çıkış fiyatı (fiyatlar 1M token başına USD); görsel: adet × görsel fiyatı. Token sayısı bilinmiyorsa giriş token'ı
/// gönderilen metinden tahmin edilir (<see cref="EstimateTokens"/>: 4 karakter ≈ 1 token, yukarı yuvarlanır, en az 1).
/// </summary>
public static class SpendCost
{
    private const decimal Million = 1_000_000m;

    /// <summary>Metin çağrısının maliyeti.</summary>
    /// <param name="model">Model (çağrı anındaki katalog fiyatı).</param>
    /// <param name="inputTokens">Giriş token.</param>
    /// <param name="outputTokens">Çıkış token.</param>
    /// <returns>USD.</returns>
    public static decimal Text(ModelEntry model, int inputTokens, int outputTokens)
    {
        ArgumentNullException.ThrowIfNull(model);
        return (inputTokens * (model.InputPricePerMillionUsd ?? 0m) / Million)
            + (outputTokens * (model.OutputPricePerMillionUsd ?? 0m) / Million);
    }

    /// <summary>Görsel çağrısının maliyeti.</summary>
    /// <param name="model">Model.</param>
    /// <param name="imageCount">Görsel adedi.</param>
    /// <returns>USD.</returns>
    public static decimal Image(ModelEntry model, int imageCount)
    {
        ArgumentNullException.ThrowIfNull(model);
        return imageCount * (model.PricePerImageUsd ?? 0m);
    }

    /// <summary>Gönderilen metnin token tahmini (Req 13).</summary>
    /// <param name="text">Gönderilen metin.</param>
    /// <returns>En az 1.</returns>
    public static int EstimateTokens(string? text) => Math.Max(1, ((text?.Length ?? 0) + 3) / 4);
}
