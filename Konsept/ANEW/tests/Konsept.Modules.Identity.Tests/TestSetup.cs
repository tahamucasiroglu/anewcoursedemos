using System.Runtime.CompilerServices;
using Konsept.Testing;

namespace Konsept.Modules.Identity.Tests;

/// <summary>
/// Test derlemesi yüklenirken kültürü <c>tr-TR</c>'ye sabitler (Türkçe I/İ tuzağını testler yakalasın).
/// </summary>
internal static class TestSetup
{
    /// <summary>Modül başlatıcısı.</summary>
    [ModuleInitializer]
    internal static void Initialize() => TestCulture.Apply();
}
