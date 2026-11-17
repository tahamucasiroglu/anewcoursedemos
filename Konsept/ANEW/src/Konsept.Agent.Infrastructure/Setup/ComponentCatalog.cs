using System.Text.Json;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Rules;

namespace Konsept.Agent.Infrastructure.Setup;

/// <summary>
/// Ajana gömülü bileşen listesi (spec 0009 Req 2, 11): <c>Setup/components.json</c> — sabit sürüm, resmi adres ve SHA-256. Liste
/// sırası kurulum sırasıdır (bağımlılıklar önce). Yerel modeller ve ortamları isteğe bağlıdır (spec 0028); varyantlı bileşenler
/// <see cref="Load(GpuInfo)"/> ile bu bilgisayarın ekran kartına göre çözülür.
/// </summary>
public static class ComponentCatalog
{
    /// <summary>Gömülü kaynağın adı.</summary>
    public const string ResourceName = "Konsept.Agent.Infrastructure.Setup.components.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Gömülü listeyi okur.</summary>
    /// <returns>Bileşenler.</returns>
    public static IReadOnlyList<SetupComponent> Load()
    {
        using var stream = typeof(ComponentCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Bileşen listesi ajana gömülü değil.");
        return JsonSerializer.Deserialize<List<SetupComponent>>(stream, Json)
            ?? throw new InvalidOperationException("Bileşen listesi okunamadı.");
    }

    /// <summary>Gömülü listeyi okur ve varyantları ekran kartına göre çözer (spec 0028 Req 5).</summary>
    /// <param name="gpu">Ekran kartı ya da <c>null</c>.</param>
    /// <returns>Çözülmüş bileşenler.</returns>
    public static IReadOnlyList<SetupComponent> Load(GpuInfo? gpu) => ComponentRules.Resolve(Load(), gpu);
}
