using System.Runtime.CompilerServices;
using Konsept.Testing;

namespace Konsept.Api.Tests;

/// <summary>
/// Test derlemesi yüklenirken kültürü <c>tr-TR</c>'ye sabitler.
/// </summary>
internal static class TestSetup
{
    /// <summary>Modül başlatıcısı.</summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        TestCulture.Apply();

        // Test kurulumu (plan 0023 A-1): mevcut testler ajanları ürün sürümünden farklı sürümlerle kaydeder; sürüm kapısı burada kapalı,
        // kapının kendisi AgentUpdateGateTests'te açıkça açılarak sınanır.
        Environment.SetEnvironmentVariable("AgentGateway__RequireSameVersion", "false");
    }
}
