namespace OkulYonetim.Application.Features.Administration;

/// <summary>Yönetim ekranındaki şube: ad, adres, telefon ve durum (pasif şubeler de listelenir).</summary>
public sealed record BranchAdminView(Guid Id, string Name, string? Address, string? Phone, bool IsActive);
