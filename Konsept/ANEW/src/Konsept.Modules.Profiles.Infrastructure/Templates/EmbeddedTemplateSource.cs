using System.Text.Json;
using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Domain.Entities;

namespace Konsept.Modules.Profiles.Infrastructure.Templates;

/// <summary>Derlemeye gömülü hazır şablonlar (<c>Templates/templates.json</c>; spec 0003 Req 5).</summary>
public sealed class EmbeddedTemplateSource : IProfileTemplateSource
{
    /// <summary>Gömülü kaynağın adı.</summary>
    public const string ResourceName = "Konsept.Profiles.templates.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Lazy<IReadOnlyList<ProfileTemplate>> Embedded = new(Load);

    /// <inheritdoc />
    public IReadOnlyList<ProfileTemplate> Templates => Embedded.Value;

    private static IReadOnlyList<ProfileTemplate> Load()
    {
        using var stream = typeof(EmbeddedTemplateSource).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Profil şablonları derlemede yok.");
        return JsonSerializer.Deserialize<List<ProfileTemplate>>(stream, Json) ?? throw new InvalidOperationException("Profil şablonları boş.");
    }
}
