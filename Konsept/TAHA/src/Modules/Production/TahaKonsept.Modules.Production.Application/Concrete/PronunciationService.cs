using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan okunuş sözlüğü servisi (scoped).
/// </summary>
/// <param name="repository">Sözlük deposu.</param>
/// <param name="settings">Ayar servisi.</param>
/// <param name="normalizer">Okunuş düzeltici.</param>
/// <param name="agent">Ajan geçidi.</param>
/// <param name="media">Online ses geçidi.</param>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
public sealed class PronunciationService(
    IPronunciationRepository repository,
    IProductionSettingsService settings,
    ISpeechTextNormalizer normalizer,
    IAgentGateway agent,
    IMediaGateway media,
    ICurrentUser currentUser)
    : PronunciationServiceBase(repository, settings, normalizer, agent, media, currentUser);
