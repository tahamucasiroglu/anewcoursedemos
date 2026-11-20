namespace OkulYonetim.Application.Features.Organization;

/// <summary>Okulun bugünü (Türkiye günü) ve verilen dönem için okul günü olup olmadığı (spec 0010 C-6).</summary>
public sealed record SchoolDay(DateOnly Today, bool IsSchoolDay);
