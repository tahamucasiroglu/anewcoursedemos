using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Base;

/// <summary>
/// Katalog sorgu davranışı (bulma, süzme, OpenRouter geçiş kimliği). Somut sınıf yalnızca kayıtları yükler (<see cref="All"/>).
/// </summary>
public abstract class ModelCatalogBase : IModelCatalog
{
    /// <summary>OpenRouter geçiş kimliği öneki (<c>openrouter:&lt;sağlayıcı/model&gt;</c>).</summary>
    public const string OpenRouterPrefix = "openrouter:";

    /// <inheritdoc />
    public abstract IReadOnlyList<ModelInfo> All { get; }

    /// <inheritdoc />
    public ModelInfo? Find(string modelKey)
    {
        if (string.IsNullOrWhiteSpace(modelKey))
        {
            return null;
        }

        var key = modelKey.Trim();
        if (key.StartsWith(OpenRouterPrefix, StringComparison.OrdinalIgnoreCase) && key.Length > OpenRouterPrefix.Length)
        {
            var passthrough = All.FirstOrDefault(m => m.Platform == ProviderPlatforms.OpenRouter);
            return passthrough is null
                ? null
                : passthrough with { Key = OpenRouterPrefix + key[OpenRouterPrefix.Length..], ProviderModelId = key[OpenRouterPrefix.Length..] };
        }

        return All.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<ModelInfo>> Query(ModelCapability? capability, ModelRunsOn? runsOn)
    {
        IReadOnlyList<ModelInfo> list = All
            .Where(m => capability is null || m.Capability == capability)
            .Where(m => runsOn is null || m.RunsOn == runsOn)
            .ToList();
        return Result.Success(list);
    }
}
