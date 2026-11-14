using TahaKonsept.Agent.SingleInstance.Base;

namespace TahaKonsept.Agent.SingleInstance.Concrete;

/// <summary>
/// Adlandırılmış mutex (<c>Local\ad.Mutex</c>) ile tek örnek kilidi ve adlandırılmış olay (<c>Local\ad.Activate</c>)
/// ile örnekler arası "öne gel" sinyali. Dinleme arka plan iş parçacığında yapılır; Dispose durdurur ve kilidi bırakır.
/// </summary>
public sealed class NamedMutexSingleInstanceGuard : SingleInstanceGuardBase
{
    private readonly string _mutexName;
    private readonly EventWaitHandle _activateEvent;
    private readonly ManualResetEvent _stopEvent = new(false);
    private Mutex? _mutex;
    private bool _owns;
    private Thread? _listener;

    /// <summary>Oluşturur.</summary>
    /// <param name="name">Kilit adı (ör. <c>TahaKonsept.Agent</c>).</param>
    public NamedMutexSingleInstanceGuard(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _mutexName = "Local\\" + name + ".Mutex";
        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\" + name + ".Activate");
    }

    /// <inheritdoc />
    public override bool TryAcquire()
    {
        if (_owns)
        {
            return true;
        }

        _mutex ??= new Mutex(false, _mutexName);
        try
        {
            _owns = _mutex.WaitOne(TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            // Önceki örnek düzgün kapanmadan öldü; kilit artık bizde.
            _owns = true;
        }

        return _owns;
    }

    /// <inheritdoc />
    public override void SignalExistingInstance() => _activateEvent.Set();

    /// <inheritdoc />
    public override void StartListening()
    {
        if (_listener is not null)
        {
            return;
        }

        _listener = new Thread(Listen) { IsBackground = true, Name = "TahaKonsept.SingleInstance" };
        _listener.Start();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _stopEvent.Set();
        _listener?.Join(TimeSpan.FromSeconds(2));
        if (_owns && _mutex is not null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutex başka iş parçacığında alınmışsa buradan bırakılamaz; süreç kapanınca işletim sistemi bırakır.
            }
        }

        _mutex?.Dispose();
        _activateEvent.Dispose();
        _stopEvent.Dispose();
    }

    private void Listen()
    {
        var handles = new WaitHandle[] { _stopEvent, _activateEvent };
        while (WaitHandle.WaitAny(handles) != 0)
        {
            RaiseActivationRequested();
        }
    }
}
