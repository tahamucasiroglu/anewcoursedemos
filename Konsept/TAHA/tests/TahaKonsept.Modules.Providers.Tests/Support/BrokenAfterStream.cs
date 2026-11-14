namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// Verilen baytları okuttuktan sonra <see cref="IOException"/> fırlatan salt okunur akış (gövde ortasında kopan bağlantı benzetimi;
/// gerçekte .NET <c>HttpIOException</c> fırlatır).
/// </summary>
public sealed class BrokenAfterStream : Stream
{
    private readonly MemoryStream _inner;

    /// <summary>Oluşturur.</summary>
    /// <param name="prefix">Kopmadan önce okunacak baytlar.</param>
    public BrokenAfterStream(byte[] prefix)
    {
        _inner = new MemoryStream(prefix, writable: false);
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        return read > 0 ? read : throw new IOException("Yanıt gövdesi okunurken bağlantı koptu.");
    }

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
