using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence;

namespace TahaKonsept.Modules.Diagnostics.Tests.Support;

/// <summary>
/// EF InMemory üzerinde <see cref="DiagnosticsDbContext"/> üretir (her ad ayrı veritabanı).
/// </summary>
public static class TestDb
{
    /// <summary>Seçenekleri üretir.</summary>
    /// <param name="name">Veritabanı adı.</param>
    /// <returns>Seçenekler.</returns>
    public static DbContextOptions<DiagnosticsDbContext> Options(string name) =>
        new DbContextOptionsBuilder<DiagnosticsDbContext>().UseInMemoryDatabase(name).Options;

    /// <summary>Yeni DbContext.</summary>
    /// <param name="clock">Saat.</param>
    /// <param name="name">Veritabanı adı (verilmezse rastgele).</param>
    /// <returns>DbContext.</returns>
    public static DiagnosticsDbContext Create(IClock clock, string? name = null) =>
        new(Options(name ?? Guid.NewGuid().ToString("N")), clock);
}
