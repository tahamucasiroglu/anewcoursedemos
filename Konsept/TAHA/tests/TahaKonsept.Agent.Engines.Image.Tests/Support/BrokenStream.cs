namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Belirli bayt sayısından sonra <see cref="IOException"/> fırlatan akış (bağlantı kopması benzetimi).</summary>
public sealed class BrokenStream : MemoryStream
{
    private readonly int _breakAfter;

    /// <summary>Akışı oluşturur.</summary>
    /// <param name="data">İçerik.</param>
    /// <param name="breakAfter">Kaç bayttan sonra kopacağı.</param>
    public BrokenStream(byte[] data, int breakAfter)
        : base(data)
    {
        _breakAfter = breakAfter;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (Position >= _breakAfter)
        {
            throw new IOException("Bağlantı koptu (test).");
        }

        return base.Read(buffer, offset, (int)Math.Min(count, _breakAfter - Position));
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (Position >= _breakAfter)
        {
            throw new IOException("Bağlantı koptu (test).");
        }

        return base.ReadAsync(buffer[..(int)Math.Min(buffer.Length, _breakAfter - Position)], cancellationToken);
    }
}
