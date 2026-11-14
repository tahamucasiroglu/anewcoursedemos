using System.Collections.Concurrent;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Sahte online görsel/ses geçidi: görselde küçük PNG baytları, seste metin uzunluğuna göre (karakter başına 0.05 sn) sessiz WAV döner.
/// </summary>
public sealed class FakeMediaGateway : IMediaGateway
{
    /// <summary>Görsel istekleri.</summary>
    public ConcurrentQueue<ImageRequest> ImageRequests { get; } = new();

    /// <summary>Ses istekleri.</summary>
    public ConcurrentQueue<SpeechRequest> SpeechRequests { get; } = new();

    /// <inheritdoc />
    public Task<Result<ImageResult>> GenerateImageAsync(string modelKey, ImageRequest request, CancellationToken ct)
    {
        ImageRequests.Enqueue(request);
        IReadOnlyList<byte[]> images = [[0x89, 0x50, 0x4E, 0x47, 1, 2, 3]];
        return Task.FromResult(Result.Success(new ImageResult(images, "image/png", modelKey, 0.04m, 5)));
    }

    /// <inheritdoc />
    public Task<Result<SpeechResult>> SynthesizeSpeechAsync(string modelKey, SpeechRequest request, CancellationToken ct)
    {
        SpeechRequests.Enqueue(request);
        var seconds = Math.Max(0.5, request.Text.Length * 0.05);
        return Task.FromResult(Result.Success(new SpeechResult(TestWav.Silence(seconds), "audio/wav", (decimal)seconds, modelKey, 0.01m, 5)));
    }
}
