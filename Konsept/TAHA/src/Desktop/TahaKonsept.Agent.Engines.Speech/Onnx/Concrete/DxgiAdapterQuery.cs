using System.Runtime.InteropServices;

namespace TahaKonsept.Agent.Engines.Speech.Onnx.Concrete;

/// <summary>
/// DXGI ile "yüksek performanslı" ekran kartının adını okur (yalnız Windows; COM vtable işlev göstericileri, ek paket yok).
/// <see cref="HighPerformanceAdapterName"/>: <c>IDXGIFactory6.EnumAdapterByGpuPreference(HIGH_PERFORMANCE)</c> sırasındaki ilk
/// yazılım olmayan adaptörün açıklaması — DirectML'in <c>performance_preference=high_performance</c> + <c>device_filter=gpu</c>
/// seçimiyle aynı sıra (Optimus dizüstüde RTX, tümleşik GPU değil). DXGI yoksa/eskiyse ya da hata olursa <c>null</c> (yalnız loglama içindir).
/// </summary>
internal static partial class DxgiAdapterQuery
{
    private const int HighPerformance = 2;              // DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE (0 = belirtilmemiş, 1 = en az güç)
    private const uint SoftwareAdapterFlag = 2;         // DXGI_ADAPTER_FLAG_SOFTWARE
    private const int EnumAdapterByGpuPreferenceSlot = 29; // IUnknown 3 + IDXGIObject 4 + Factory 5 + Factory1 2 + Factory2 11 + Factory3 1 + Factory4 2 + Factory5 1
    private const int GetDesc1Slot = 10;                // IUnknown 3 + IDXGIObject 4 + IDXGIAdapter 3
    private const int ReleaseSlot = 2;
    private const int DescSize = 312;                   // DXGI_ADAPTER_DESC1 (x64)
    private const int DescFlagsOffset = 304;            // WCHAR[128] + 4×UINT + 3×SIZE_T + LUID

    private static readonly Guid IidFactory6 = new("c1b6694f-ff09-44a9-b03c-77900a0a1d17");
    private static readonly Guid IidAdapter1 = new("29038f61-3839-4626-91fd-086879011a05");

    /// <summary>Yüksek performans sırasındaki ilk donanım adaptörünün adı.</summary>
    /// <returns>Adaptör adı; okunamazsa <c>null</c>.</returns>
    public static string? HighPerformanceAdapterName()
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
        {
            return null;
        }

        try
        {
            return Query();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or SEHException)
        {
            return null;
        }
    }

    private static unsafe string? Query()
    {
        nint factory = 0;
        var iidFactory = IidFactory6;
        if (CreateDXGIFactory1(&iidFactory, &factory) < 0 || factory == 0)
        {
            return null;
        }

        try
        {
            var enumByPreference = (delegate* unmanaged[Stdcall]<nint, uint, int, Guid*, nint*, int>)(*(nint**)factory)[EnumAdapterByGpuPreferenceSlot];
            for (uint i = 0; i < 16; i++)
            {
                nint adapter = 0;
                var iidAdapter = IidAdapter1;
                if (enumByPreference(factory, i, HighPerformance, &iidAdapter, &adapter) < 0 || adapter == 0)
                {
                    return null;
                }

                try
                {
                    var getDesc1 = (delegate* unmanaged[Stdcall]<nint, byte*, int>)(*(nint**)adapter)[GetDesc1Slot];
                    var desc = stackalloc byte[DescSize];
                    if (getDesc1(adapter, desc) < 0 || (*(uint*)(desc + DescFlagsOffset) & SoftwareAdapterFlag) != 0)
                    {
                        continue;
                    }

                    var name = new string((char*)desc).Trim();
                    return name.Length > 0 ? name : null;
                }
                finally
                {
                    Release(adapter);
                }
            }

            return null;
        }
        finally
        {
            Release(factory);
        }
    }

    private static unsafe void Release(nint comObject) =>
        ((delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)comObject)[ReleaseSlot])(comObject);

    [LibraryImport("dxgi.dll")]
    private static unsafe partial int CreateDXGIFactory1(Guid* riid, nint* factory);
}
