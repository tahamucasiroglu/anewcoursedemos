using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Abstractions.Image.Abstract;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Abstractions.Video.Abstract;
using TahaKonsept.Agent.EngineList.Base;

namespace TahaKonsept.Agent.EngineList.Concrete;

/// <summary>
/// Motorları doğrudan türlü kayıtlardan (<see cref="IVideoEngine"/>, <see cref="ISpeechEngine"/>, <see cref="IImageEngine"/>) toplar;
/// böylece motor projelerinin ayrıca <see cref="IAgentEngine"/> kaydı yapması gerekmez.
/// </summary>
public sealed class RegisteredEngineCatalog : EngineCatalogBase
{
    private readonly IEnumerable<IVideoEngine> _video;
    private readonly IEnumerable<ISpeechEngine> _speech;
    private readonly IEnumerable<IImageEngine> _image;

    /// <summary>Oluşturur.</summary>
    /// <param name="video">Video motorları.</param>
    /// <param name="speech">Ses motorları.</param>
    /// <param name="image">Görsel motorları.</param>
    public RegisteredEngineCatalog(IEnumerable<IVideoEngine> video, IEnumerable<ISpeechEngine> speech, IEnumerable<IImageEngine> image)
    {
        _video = video;
        _speech = speech;
        _image = image;
    }

    /// <inheritdoc />
    protected override IEnumerable<(EngineCategory Category, IEnumerable<IAgentEngine> Engines)> GetGroups()
    {
        yield return (EngineCategory.Video, _video);
        yield return (EngineCategory.Speech, _speech);
        yield return (EngineCategory.Image, _image);
    }
}
