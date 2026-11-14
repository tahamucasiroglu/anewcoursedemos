using System.Runtime.CompilerServices;
using Konsept.Testing;

namespace Konsept.Shared.Logging.Tests;

/// <summary>
/// Test derlemesi yüklenirken kültürü <c>tr-TR</c>'ye sabitler.
/// </summary>
internal static class TestSetup
{
    /// <summary>Modül başlatıcısı.</summary>
    [ModuleInitializer]
    internal static void Initialize() => TestCulture.Apply();
}
