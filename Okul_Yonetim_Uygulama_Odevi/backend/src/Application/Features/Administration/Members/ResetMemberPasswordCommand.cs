using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>Kurucunun şifre sıfırlaması (spec 0005 gereksinim 10). Yanıtı geçici şifre taşır; loglanmaz (T9).</summary>
public sealed record ResetMemberPasswordCommand(Guid UserId) : IRequest<ResetMemberPasswordResult>;
