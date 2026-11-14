namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Ölçülmüş (CallLog'a yazılmış) bir sağlayıcı çağrısının çıktısı: değer + maliyet + süre + deneme sayısı.
/// </summary>
/// <typeparam name="T">Çıktı türü.</typeparam>
/// <param name="Value">Çıktı.</param>
/// <param name="CostUsd">Maliyet ($).</param>
/// <param name="DurationMs">Son denemenin süresi (ms).</param>
/// <param name="Attempts">Deneme sayısı (geçici hatalarda yeniden deneme).</param>
public sealed record Metered<T>(T Value, decimal CostUsd, long DurationMs, int Attempts);
