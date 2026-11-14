namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>Satır okuması iptal edilene kadar asılı kalan okuyucu (yanıtı geç gelen işçi benzetimi).</summary>
public sealed class BlockingTextReader : TextReader
{
    /// <inheritdoc />
    public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        return null;
    }
}
