using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Concepts.Tests.Support;

/// <summary>
/// Sahte LLM geçidi: sıraya konan yanıtları (<see cref="Enqueue{T}"/>) ya da hataları (<see cref="EnqueueError"/>) sırayla döner,
/// her çağrının <see cref="TextTask"/>'ını <see cref="Calls"/>'a yazar. Her başarılı yanıt 100 giriş / 50 çıkış token'ı sayar.
/// </summary>
public sealed class FakeProviderGateway : IProviderGateway
{
    private readonly Queue<object> _responses = new();

    /// <summary>Yapılan çağrılar.</summary>
    public List<TextTask> Calls { get; } = [];

    /// <summary>Başarılı yanıt ekler.</summary>
    /// <typeparam name="T">Değer türü.</typeparam>
    /// <param name="value">Değer.</param>
    /// <param name="cost">Maliyet.</param>
    public void Enqueue<T>(T value, decimal cost = 0.01m) =>
        _responses.Enqueue(Result.Success(new TextResult<T>
        {
            Value = value,
            ModelKey = "fake-model",
            CostUsd = cost,
            Usage = new CallUsage { InputTokens = 100, OutputTokens = 50 },
            DurationMs = 10,
        }));

    /// <summary>Hata yanıtı ekler.</summary>
    /// <param name="error">Hata.</param>
    public void EnqueueError(Error error) => _responses.Enqueue(error);

    /// <summary>Sıradaki yanıtı döner.</summary>
    /// <typeparam name="T">Değer türü.</typeparam>
    /// <param name="task">İş.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result<TextResult<T>>> RunStructuredAsync<T>(TextTask task, CancellationToken ct)
    {
        Calls.Add(task);
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("Sahte geçitte yanıt kalmadı.");
        }

        var next = _responses.Dequeue();
        return Task.FromResult(next switch
        {
            Error e => Result.Failure<TextResult<T>>(e),
            Result<TextResult<T>> r => r,
            _ => throw new InvalidOperationException($"Beklenen yanıt türü {typeof(T).Name}, sıradaki {next.GetType().Name}."),
        });
    }

    /// <summary>Desteklenmez.</summary>
    /// <param name="task">İş.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yok.</returns>
    public Task<Result<TextResult<string>>> RunTextAsync(TextTask task, CancellationToken ct) => throw new NotSupportedException();
}
