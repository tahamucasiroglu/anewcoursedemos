namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>Testte açılan okul: kurum, ilk şube, kurucu kişi, kurucunun e-postası ve okul adı.</summary>
public sealed record SchoolSeed(Guid TenantId, Guid BranchId, Guid FounderId, string FounderEmail, string SchoolName);
