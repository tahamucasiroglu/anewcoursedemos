using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Shared.Contracts.Common;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IStageInputHasher"/> ortak davranışı: girdiyi ortak sözleşme ayarlarıyla kanonik JSON'a çevirir (özellik sırası tür tanımından,
/// enum metin), aşama adı ve sahne ile birleştirip <see cref="Digest"/> ile özetler. Somut sınıf özet algoritmasını seçer.
/// </summary>
public abstract class StageInputHasherBase : IStageInputHasher
{
    /// <summary>Girdi hash'i.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <param name="input">Hash girdisi.</param>
    /// <returns>Hash.</returns>
    public string HashInput(PipelineStage stage, int? sceneIndex, object input)
    {
        var json = JsonSerializer.Serialize(input, input?.GetType() ?? typeof(object), ContractJson.Options);
        return Digest("in|" + stage + "|" + (sceneIndex?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-") + "|" + json);
    }

    /// <summary>Çıktı hash'i (çıktı yoksa girdi hash'inden türetilir; böylece aynı girdinin çıktısı aynı kimliği alır).</summary>
    /// <param name="inputHash">Girdi hash'i.</param>
    /// <param name="outputJson">Çıktı JSON'u.</param>
    /// <returns>Hash.</returns>
    public string HashOutput(string inputHash, string? outputJson) =>
        Digest(string.IsNullOrEmpty(outputJson) ? "out|noop|" + inputHash : "out|" + outputJson);

    /// <summary>Metni özetler.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Onaltılık küçük harf özet.</returns>
    protected abstract string Digest(string text);

    /// <summary>SHA-256 özeti (somut sınıflar için yardımcı).</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Onaltılık küçük harf özet.</returns>
    protected static string Sha256Hex(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
