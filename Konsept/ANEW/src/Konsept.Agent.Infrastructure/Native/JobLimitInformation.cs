using System.Runtime.InteropServices;

namespace Konsept.Agent.Infrastructure.Native;

/// <summary>JOBOBJECT_EXTENDED_LIMIT_INFORMATION (x64 düzeni; temel sınırlar + IO sayaçları + bellek alanları).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct JobLimitInformation
{
    /// <summary>PerProcessUserTimeLimit.</summary>
    public long PerProcessUserTimeLimit;

    /// <summary>PerJobUserTimeLimit.</summary>
    public long PerJobUserTimeLimit;

    /// <summary>LimitFlags.</summary>
    public uint LimitFlags;

    /// <summary>MinimumWorkingSetSize.</summary>
    public UIntPtr MinimumWorkingSetSize;

    /// <summary>MaximumWorkingSetSize.</summary>
    public UIntPtr MaximumWorkingSetSize;

    /// <summary>ActiveProcessLimit.</summary>
    public uint ActiveProcessLimit;

    /// <summary>Affinity.</summary>
    public UIntPtr Affinity;

    /// <summary>PriorityClass.</summary>
    public uint PriorityClass;

    /// <summary>SchedulingClass.</summary>
    public uint SchedulingClass;

    /// <summary>IO_COUNTERS.ReadOperationCount.</summary>
    public ulong ReadOperationCount;

    /// <summary>IO_COUNTERS.WriteOperationCount.</summary>
    public ulong WriteOperationCount;

    /// <summary>IO_COUNTERS.OtherOperationCount.</summary>
    public ulong OtherOperationCount;

    /// <summary>IO_COUNTERS.ReadTransferCount.</summary>
    public ulong ReadTransferCount;

    /// <summary>IO_COUNTERS.WriteTransferCount.</summary>
    public ulong WriteTransferCount;

    /// <summary>IO_COUNTERS.OtherTransferCount.</summary>
    public ulong OtherTransferCount;

    /// <summary>ProcessMemoryLimit.</summary>
    public UIntPtr ProcessMemoryLimit;

    /// <summary>JobMemoryLimit.</summary>
    public UIntPtr JobMemoryLimit;

    /// <summary>PeakProcessMemoryUsed.</summary>
    public UIntPtr PeakProcessMemoryUsed;

    /// <summary>PeakJobMemoryUsed.</summary>
    public UIntPtr PeakJobMemoryUsed;
}
