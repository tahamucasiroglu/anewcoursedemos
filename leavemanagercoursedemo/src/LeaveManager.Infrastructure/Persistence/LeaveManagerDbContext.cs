using Microsoft.EntityFrameworkCore;

namespace LeaveManager.Infrastructure.Persistence;

/// <summary>The application's database context. Empty until the first entity (spec 0002).</summary>
public sealed class LeaveManagerDbContext(DbContextOptions<LeaveManagerDbContext> options)
    : DbContext(options)
{
}
