using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Identity.Queries.UserExists;

/// <summary>E-postayı karşılaştırma biçimine çevirip kişiyi arar.</summary>
public sealed class UserExistsQueryHandler : IRequestHandler<UserExistsQuery, bool>
{
    private readonly IUserRepository users;

    public UserExistsQueryHandler(IUserRepository users)
    {
        this.users = users;
    }

    public async Task<bool> Handle(UserExistsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await this.users.FindByEmailAsync(EmailAddress.Normalize(request.Email), cancellationToken).ConfigureAwait(false) is not null;
    }
}
