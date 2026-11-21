using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace OkulYonetim.Tests.Unit.Errors;

/// <summary>
/// Yanıt özelliğinin test sarmalayıcısı (T19, review round 2 Düşük-2): durum kodu ilk kez <c>blockOn</c> değerine
/// ayarlanınca değer yazılır, <see cref="Entered"/> tamamlanır ve test <see cref="Release"/> diyene kadar (en fazla
/// <see cref="Wait"/>) bekletilir. Hata işleyici durum kodunu yazıp gövdeyi yazmadan önce durur; bu arada ikinci istek
/// işlenir — paylaşılan işleyicide ikinci isteğin yanıtı birinci isteğin gövdesini de alır. Diğer üyeler iç özelliğe aktarılır.
/// </summary>
public sealed class BlockingStatusResponseFeature : IHttpResponseFeature
{
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private readonly HttpResponseFeature inner = new();
    private readonly int blockOn;
    private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int blocked;

    public BlockingStatusResponseFeature(int blockOn)
    {
        this.blockOn = blockOn;
    }

    public Task Entered => this.entered.Task;

    public int StatusCode
    {
        get => this.inner.StatusCode;
        set
        {
            this.inner.StatusCode = value;
            if (value == this.blockOn && Interlocked.Exchange(ref this.blocked, 1) == 0)
            {
                this.entered.TrySetResult();
                this.released.Task.Wait(Wait);
            }
        }
    }

    public string? ReasonPhrase
    {
        get => this.inner.ReasonPhrase;
        set => this.inner.ReasonPhrase = value;
    }

    public IHeaderDictionary Headers
    {
        get => this.inner.Headers;
        set => this.inner.Headers = value;
    }

    public Stream Body { get; set; } = Stream.Null;

    public bool HasStarted => false;

    public void Release() => this.released.TrySetResult();

    public void OnStarting(Func<object, Task> callback, object state) => this.inner.OnStarting(callback, state);

    public void OnCompleted(Func<object, Task> callback, object state) => this.inner.OnCompleted(callback, state);
}
