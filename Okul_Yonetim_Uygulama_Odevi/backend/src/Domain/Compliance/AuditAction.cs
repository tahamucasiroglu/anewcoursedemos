namespace OkulYonetim.Domain.Compliance;

/// <summary>Audit kaydına geçen olay türleri (spec 0004, gereksinim 13); sonraki spec'ler yeni olay ekler.</summary>
public enum AuditAction
{
    LoginSucceeded = 1,
    LoginFailed = 2,
    LoginLockedOut = 3,
    Logout = 4,
    TenantSelected = 5,
    TenantCreated = 6,
    BranchCreated = 7,
    UserCreated = 8,
    RoleAssigned = 9,
    RefreshTokenReused = 10,
    BranchUpdated = 11,
    BranchDeactivated = 12,
    BranchActivated = 13,
    MemberProfileUpdated = 14,
    RoleRevoked = 15,
    MemberDeactivated = 16,
    MemberActivated = 17,
    PasswordReset = 18,
    MemberAdded = 19,
    ProfileUpdated = 20,
    PasswordChanged = 21,
    PasswordChangeFailed = 22,
    PasswordResetRequested = 23,
    PasswordResetCompleted = 24,
    PasswordResetLinkInvalid = 25,
}
