using TahaKonsept.Agent.Account.Base;
using TahaKonsept.Agent.Core.Auth.Abstract;

namespace TahaKonsept.Agent.Account.Concrete;

/// <summary>Hesap adını çekirdeğin DPAPI ile şifreli token deposundaki (<see cref="ITokenStore"/>) erişim token'ından okur.</summary>
public sealed class TokenStorePairedAccountReader : PairedAccountReaderBase
{
    private readonly ITokenStore _tokens;

    /// <summary>Oluşturur.</summary>
    /// <param name="tokens">Token deposu.</param>
    public TokenStorePairedAccountReader(ITokenStore tokens) => _tokens = tokens;

    /// <inheritdoc />
    protected override string? GetAccessToken() => _tokens.Load()?.AccessToken;
}
