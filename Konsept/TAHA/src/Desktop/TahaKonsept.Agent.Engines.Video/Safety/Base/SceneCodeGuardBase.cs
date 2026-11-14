using TahaKonsept.Agent.Engines.Video.Safety.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Safety.Base;

/// <summary>
/// Kural listesine dayalı <see cref="ISceneCodeGuard"/> tabanı. Türetilen sınıf <see cref="Rules"/> listesini verir;
/// boş kod ve aşırı büyük kod (<see cref="MaxCodeLength"/>) da reddedilir.
/// </summary>
public abstract class SceneCodeGuardBase : ISceneCodeGuard
{
    /// <summary>İzin verilen en büyük kod uzunluğu (karakter).</summary>
    protected virtual int MaxCodeLength => 400_000;

    /// <summary>Uygulanacak kurallar.</summary>
    protected abstract IReadOnlyList<SceneCodeRule> Rules { get; }

    /// <inheritdoc />
    public Result Check(string sceneCode)
    {
        if (string.IsNullOrWhiteSpace(sceneCode))
        {
            return Error.Validation("scene.code_empty", "Sahne kodu boş.");
        }

        if (sceneCode.Length > MaxCodeLength)
        {
            return Error.Validation("scene.code_too_large", $"Sahne kodu çok büyük ({sceneCode.Length} karakter, sınır {MaxCodeLength}).");
        }

        var violations = Rules
            .Where(r => r.Pattern.IsMatch(sceneCode))
            .Select(r => r.Message)
            .ToList();

        return violations.Count == 0
            ? Result.Success()
            : Error.Validation("scene.code_unsafe", "Sahne kodu güvenlik kurallarına uymuyor:\n- " + string.Join("\n- ", violations));
    }
}
