using System.Runtime.InteropServices;

namespace Konsept.Agent.Infrastructure.Native;

/// <summary>JOBOBJECT_ASSOCIATE_COMPLETION_PORT: sınır ihlali bildirimleri (etkin süreç, bellek) bu porta gelir.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct JobCompletionPort
{
    /// <summary>CompletionKey.</summary>
    public IntPtr CompletionKey;

    /// <summary>CompletionPort.</summary>
    public IntPtr CompletionPort;
}
