using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Identity.Domain.Enums;

namespace TahaKonsept.Modules.Identity.Domain.Entities;

/// <summary>
/// Dönen (rotating) refresh token kaydı. Ham token DB'ye yazılmaz; yalnızca SHA-256 özeti (<see cref="TokenHash"/>) saklanır.
/// Aynı girişten türeyen token'lar bir aileyi paylaşır (<see cref="FamilyId"/>); ailede kullanılmış bir token tekrar gelirse
/// aile tamamen iptal edilir. <see cref="Kind"/> web / ajan ayrımıdır. Davranışlar: <see cref="Issue"/>, <see cref="IsActive"/>,
/// <see cref="Revoke"/>.
/// </summary>
public class RefreshToken : AggregateRoot
{
    /// <summary>EF için boş oluşturucu.</summary>
    protected RefreshToken()
    {
        TokenHash = string.Empty;
    }

    private RefreshToken(Guid userId, string tokenHash, Guid familyId, RefreshTokenKind kind, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        Kind = kind;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Sahibi olan kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Ham token'ın SHA-256 özeti (hex).</summary>
    public string TokenHash { get; private set; }

    /// <summary>Token ailesi (aynı girişten dönen token'lar).</summary>
    public Guid FamilyId { get; private set; }

    /// <summary>İstemci türü.</summary>
    public RefreshTokenKind Kind { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Bitiş anı (UTC).</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>İptal anı (UTC); etkinse null.</summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>İptal nedeni.</summary>
    public TokenRevocationReason? RevocationReason { get; private set; }

    /// <summary>Rotasyonda yerine geçen token'ın kimliği.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>İptal edilmiş mi.</summary>
    public bool IsRevoked => RevokedAtUtc.HasValue;

    /// <summary>Yeni refresh token kaydı üretir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenHash">Ham token özeti.</param>
    /// <param name="familyId">Aile.</param>
    /// <param name="kind">İstemci türü.</param>
    /// <param name="nowUtc">Şu an (UTC).</param>
    /// <param name="lifetime">Geçerlilik süresi.</param>
    /// <returns>Kayıt.</returns>
    public static RefreshToken Issue(Guid userId, string tokenHash, Guid familyId, RefreshTokenKind kind, DateTimeOffset nowUtc, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new RefreshToken(userId, tokenHash, familyId, kind, nowUtc, nowUtc.Add(lifetime));
    }

    /// <summary>İptal edilmemiş ve süresi dolmamış mı.</summary>
    /// <param name="nowUtc">Şu an (UTC).</param>
    /// <returns>Etkinse true.</returns>
    public bool IsActive(DateTimeOffset nowUtc) => !IsRevoked && ExpiresAtUtc > nowUtc;

    /// <summary>Token'ı iptal eder (zaten iptalse dokunmaz).</summary>
    /// <param name="nowUtc">Şu an (UTC).</param>
    /// <param name="reason">Neden.</param>
    /// <param name="replacedByTokenId">Rotasyonda yerine geçen token.</param>
    public void Revoke(DateTimeOffset nowUtc, TokenRevocationReason reason, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = nowUtc;
        RevocationReason = reason;
        ReplacedByTokenId = replacedByTokenId;
    }
}
