using System.Text.Json;

namespace TahaKonsept.Agent.Engines.Speech.Supertonic;

/// <summary>
/// Supertonic <c>onnx/tts.json</c> dosyasından okunan, çıkarım için gereken değerler:
/// örnekleme hızı, temel parça (chunk) boyu, sıkıştırma çarpanı ve gizil (latent) boyut. <see cref="Load"/> dosyadan okur.
/// </summary>
/// <param name="SampleRate">Çıkış örnekleme hızı (ae.sample_rate, 44100).</param>
/// <param name="BaseChunkSize">ae.base_chunk_size.</param>
/// <param name="ChunkCompressFactor">ttl.chunk_compress_factor.</param>
/// <param name="LatentDim">ttl.latent_dim.</param>
internal sealed record SupertonicConfig(int SampleRate, int BaseChunkSize, int ChunkCompressFactor, int LatentDim)
{
    /// <summary><c>tts.json</c> dosyasını okur.</summary>
    /// <param name="path">Dosya yolu.</param>
    /// <returns>Yapılandırma.</returns>
    public static SupertonicConfig Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var ae = root.GetProperty("ae");
        var ttl = root.GetProperty("ttl");
        return new SupertonicConfig(
            ae.GetProperty("sample_rate").GetInt32(),
            ae.GetProperty("base_chunk_size").GetInt32(),
            ttl.GetProperty("chunk_compress_factor").GetInt32(),
            ttl.GetProperty("latent_dim").GetInt32());
    }
}
