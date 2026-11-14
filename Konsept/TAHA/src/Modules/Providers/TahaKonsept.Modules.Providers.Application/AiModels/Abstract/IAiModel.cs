using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.AiModels.Abstract;

/// <summary>
/// Tüm online modellerin ortak sözleşmesi (metin / görsel / ses dalları buradan türer).
/// </summary>
public interface IAiModel
{
    /// <summary>Platform kimliği (ör. <c>anthropic</c>).</summary>
    string PlatformName { get; }

    /// <summary>Sağlayıcı model kimliği.</summary>
    string ModelId { get; }

    /// <summary>Katalog anahtarı.</summary>
    string ModelKey { get; }

    /// <summary>Görünen ad.</summary>
    string DisplayName { get; }

    /// <summary>Yetenek.</summary>
    ModelCapability Capability { get; }
}
