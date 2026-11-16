using System.Runtime.InteropServices;

namespace Konsept.Agent.Infrastructure.Native;

/// <summary>JOBOBJECT_BASIC_ACCOUNTING_INFORMATION.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct JobAccounting
{
    /// <summary>TotalUserTime.</summary>
    public long TotalUserTime;

    /// <summary>TotalKernelTime.</summary>
    public long TotalKernelTime;

    /// <summary>ThisPeriodTotalUserTime.</summary>
    public long ThisPeriodTotalUserTime;

    /// <summary>ThisPeriodTotalKernelTime.</summary>
    public long ThisPeriodTotalKernelTime;

    /// <summary>TotalPageFaultCount.</summary>
    public uint TotalPageFaultCount;

    /// <summary>TotalProcesses.</summary>
    public uint TotalProcesses;

    /// <summary>ActiveProcesses.</summary>
    public uint ActiveProcesses;

    /// <summary>TotalTerminatedProcesses.</summary>
    public uint TotalTerminatedProcesses;
}
