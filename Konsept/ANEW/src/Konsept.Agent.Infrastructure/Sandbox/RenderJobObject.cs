using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Infrastructure.Native;
using Microsoft.Win32.SafeHandles;

namespace Konsept.Agent.Infrastructure.Sandbox;

/// <summary>
/// Render süreç ağacının Job Object'i (spec 0018 Req 1, L4): kapatılınca tüm süreçler ölür (kill-on-close), aynı anda en çok
/// <see cref="RenderLimits.MaxProcesses"/> süreç, toplam bellek <see cref="RenderLimits.MemoryBytes"/>. Sınır ihlali bildirimleri bir tamamlanma
/// portundan okunur. Yönetici yetkisi istemez.
/// </summary>
public sealed class RenderJobObject : IDisposable
{
    private readonly SafeFileHandle _job;
    private readonly SafeFileHandle _port;
    private readonly long _memoryLimit;
    private bool _limitHit;

    private RenderJobObject(SafeFileHandle job, SafeFileHandle port, long memoryLimit)
    {
        _job = job;
        _port = port;
        _memoryLimit = memoryLimit;
    }

    /// <summary>Gets etkin (yaşayan) süreç sayısı.</summary>
    public int ActiveProcesses => (int)Accounting().ActiveProcesses;

    /// <summary>Job oluşturur ve sınırları uygular.</summary>
    /// <param name="limits">Sınırlar.</param>
    /// <returns>Job.</returns>
    public static RenderJobObject Create(RenderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        var job = JobNativeMethods.CreateJobObject(IntPtr.Zero, IntPtr.Zero);
        if (job.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var port = JobNativeMethods.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, IntPtr.Zero, 1);
        var info = new JobLimitInformation
        {
            LimitFlags = JobNativeMethods.LimitKillOnJobClose | JobNativeMethods.LimitActiveProcess | JobNativeMethods.LimitJobMemory
                | JobNativeMethods.LimitDieOnUnhandledException,
            ActiveProcessLimit = (uint)limits.MaxProcesses,
            JobMemoryLimit = (UIntPtr)(ulong)limits.MemoryBytes,
        };
        var association = new JobCompletionPort { CompletionKey = IntPtr.Zero, CompletionPort = port.DangerousGetHandle() };
        if (port.IsInvalid
            || !JobNativeMethods.SetInformationJobObject(job, JobNativeMethods.ExtendedLimitInformation, ref info, (uint)Marshal.SizeOf<JobLimitInformation>())
            || !JobNativeMethods.SetInformationJobObject(job, JobNativeMethods.AssociateCompletionPortInformation, ref association, (uint)Marshal.SizeOf<JobCompletionPort>()))
        {
            var error = Marshal.GetLastWin32Error();
            job.Dispose();
            port.Dispose();
            throw new Win32Exception(error);
        }

        return new RenderJobObject(job, port, limits.MemoryBytes);
    }

    /// <summary>Süreci Job'a ekler (alt süreçleri de içinde doğar).</summary>
    /// <param name="process">Süreç.</param>
    public void Assign(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (!JobNativeMethods.AssignProcessToJobObject(_job, process.Handle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    /// <summary>Job'daki tüm süreçleri sonlandırır.</summary>
    public void Terminate() => JobNativeMethods.TerminateJobObject(_job, 1);

    /// <summary>Süre dışında bir sınır (süreç sayısı, bellek) aşıldı mı; port bildirimlerini ve tepe belleği okur.</summary>
    /// <returns>Aşıldıysa <c>true</c>.</returns>
    public bool LimitExceeded()
    {
        while (JobNativeMethods.GetQueuedCompletionStatus(_port, out var message, out _, out _, 0))
        {
            if (message is JobNativeMethods.MessageActiveProcessLimit or JobNativeMethods.MessageJobMemoryLimit or JobNativeMethods.MessageProcessMemoryLimit)
            {
                _limitHit = true;
            }
        }

        if (JobNativeMethods.QueryInformationJobObject(_job, JobNativeMethods.ExtendedLimitInformation, out JobLimitInformation info, (uint)Marshal.SizeOf<JobLimitInformation>(), IntPtr.Zero)
            && (long)(ulong)info.PeakJobMemoryUsed >= _memoryLimit * 9 / 10)
        {
            _limitHit = true;
        }

        return _limitHit;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _job.Dispose(); // Kill-on-close: kalan her süreç ölür.
        _port.Dispose();
    }

    private JobAccounting Accounting() =>
        JobNativeMethods.QueryInformationJobObject(_job, JobNativeMethods.BasicAccountingInformation, out JobAccounting info, (uint)Marshal.SizeOf<JobAccounting>(), IntPtr.Zero)
            ? info
            : default;
}
