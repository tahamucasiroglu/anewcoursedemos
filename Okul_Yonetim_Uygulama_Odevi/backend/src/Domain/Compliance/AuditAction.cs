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
}
