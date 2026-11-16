using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Konsept.Agent.Infrastructure.Native;

/// <summary>Job Object Win32 çağrıları (spec 0018 L4; yönetici yetkisi istemez).</summary>
internal static class JobNativeMethods
{
    /// <summary>JobObjectBasicAccountingInformation.</summary>
    public const int BasicAccountingInformation = 1;

    /// <summary>JobObjectAssociateCompletionPortInformation.</summary>
    public const int AssociateCompletionPortInformation = 7;

    /// <summary>JobObjectExtendedLimitInformation.</summary>
    public const int ExtendedLimitInformation = 9;

    /// <summary>JOB_OBJECT_LIMIT_ACTIVE_PROCESS.</summary>
    public const uint LimitActiveProcess = 0x8;

    /// <summary>JOB_OBJECT_LIMIT_JOB_MEMORY.</summary>
    public const uint LimitJobMemory = 0x200;

    /// <summary>JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION.</summary>
    public const uint LimitDieOnUnhandledException = 0x400;

    /// <summary>JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE.</summary>
    public const uint LimitKillOnJobClose = 0x2000;

    /// <summary>JOB_OBJECT_MSG_ACTIVE_PROCESS_LIMIT.</summary>
    public const uint MessageActiveProcessLimit = 3;

    /// <summary>JOB_OBJECT_MSG_PROCESS_MEMORY_LIMIT.</summary>
    public const uint MessageProcessMemoryLimit = 9;

    /// <summary>JOB_OBJECT_MSG_JOB_MEMORY_LIMIT.</summary>
    public const uint MessageJobMemoryLimit = 10;

    /// <summary>Adsız Job Object oluşturur.</summary>
    /// <param name="attributes">Güvenlik öznitelikleri (sıfır).</param>
    /// <param name="name">Ad (sıfır).</param>
    /// <returns>Tanıtıcı.</returns>
    [DllImport("kernel32.dll", EntryPoint = "CreateJobObjectW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern SafeFileHandle CreateJobObject(IntPtr attributes, IntPtr name);

    /// <summary>Sınırları yazar.</summary>
    /// <param name="job">Job.</param>
    /// <param name="infoClass">Bilgi sınıfı.</param>
    /// <param name="info">Bilgi.</param>
    /// <param name="length">Uzunluk.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetInformationJobObject(SafeFileHandle job, int infoClass, ref JobLimitInformation info, uint length);

    /// <summary>Tamamlanma portunu bağlar.</summary>
    /// <param name="job">Job.</param>
    /// <param name="infoClass">Bilgi sınıfı.</param>
    /// <param name="info">Bilgi.</param>
    /// <param name="length">Uzunluk.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetInformationJobObject(SafeFileHandle job, int infoClass, ref JobCompletionPort info, uint length);

    /// <summary>Sınır bilgisini okur (tepe bellek).</summary>
    /// <param name="job">Job.</param>
    /// <param name="infoClass">Bilgi sınıfı.</param>
    /// <param name="info">Bilgi.</param>
    /// <param name="length">Uzunluk.</param>
    /// <param name="returnLength">Dönen uzunluk.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryInformationJobObject(SafeFileHandle job, int infoClass, out JobLimitInformation info, uint length, IntPtr returnLength);

    /// <summary>Muhasebe bilgisini okur (etkin süreç sayısı).</summary>
    /// <param name="job">Job.</param>
    /// <param name="infoClass">Bilgi sınıfı.</param>
    /// <param name="info">Bilgi.</param>
    /// <param name="length">Uzunluk.</param>
    /// <param name="returnLength">Dönen uzunluk.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryInformationJobObject(SafeFileHandle job, int infoClass, out JobAccounting info, uint length, IntPtr returnLength);

    /// <summary>Süreci Job'a ekler.</summary>
    /// <param name="job">Job.</param>
    /// <param name="process">Süreç tanıtıcısı.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);

    /// <summary>Job'daki tüm süreçleri sonlandırır.</summary>
    /// <param name="job">Job.</param>
    /// <param name="exitCode">Çıkış kodu.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TerminateJobObject(SafeFileHandle job, uint exitCode);

    /// <summary>Tamamlanma portu oluşturur.</summary>
    /// <param name="fileHandle">INVALID_HANDLE_VALUE.</param>
    /// <param name="existingPort">Sıfır.</param>
    /// <param name="completionKey">Anahtar.</param>
    /// <param name="threads">İş parçacığı sayısı.</param>
    /// <returns>Port.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern SafeFileHandle CreateIoCompletionPort(IntPtr fileHandle, IntPtr existingPort, IntPtr completionKey, uint threads);

    /// <summary>Porttaki bir bildirimi okur.</summary>
    /// <param name="port">Port.</param>
    /// <param name="message">Bildirim türü (JOB_OBJECT_MSG_*).</param>
    /// <param name="key">Anahtar.</param>
    /// <param name="overlapped">Süreç kimliği.</param>
    /// <param name="milliseconds">Bekleme.</param>
    /// <returns>Bildirim varsa <c>true</c>.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetQueuedCompletionStatus(SafeFileHandle port, out uint message, out IntPtr key, out IntPtr overlapped, uint milliseconds);
}
