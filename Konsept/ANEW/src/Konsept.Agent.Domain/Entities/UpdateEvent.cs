using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Domain.Entities;

/// <summary>Güncelleme olayı (spec 0023 Req 5): durum, ilerleme ve hata sebebi; metinleri arayüz kaynak dosyasından seçer.</summary>
/// <param name="Status">Durum.</param>
/// <param name="Percent">İndirme ilerlemesi (0–100).</param>
/// <param name="Failure">Hata sebebi.</param>
/// <param name="Version">İlgili sürüm.</param>
public sealed record UpdateEvent(UpdateStatus Status, int Percent = 0, SetupFailure Failure = SetupFailure.None, string? Version = null);
