using TahaKonsept.BuildingBlocks.Api.Modules.Abstract;
using TahaKonsept.Modules.Agents.Api;
using TahaKonsept.Modules.Diagnostics.Api;
using TahaKonsept.Modules.Identity.Api;
using TahaKonsept.Modules.Profiles.Api;

namespace TahaKonsept.Api.Modules;

/// <summary>
/// API'nin barındırdığı modüllerin AÇIK listesi (yansıma ile tarama yok). Yeni modül = buraya bir satır
/// (ör. <c>new IdentityModule()</c>) + host csproj'una modülün Api projesi referansı. Sıra migration sırasıdır.
/// </summary>
internal static class ApiModules
{
    /// <summary>Modül örneklerini üretir.</summary>
    /// <returns>Modüller.</returns>
    public static IReadOnlyList<IModule> Create() =>
    [
        new IdentityModule(),
        new ProfilesModule(),
        // new ProvidersModule(),
        // new ConceptsModule(),
        // new ProductionModule(),
        new AgentsModule(),
        new DiagnosticsModule(),
    ];
}
