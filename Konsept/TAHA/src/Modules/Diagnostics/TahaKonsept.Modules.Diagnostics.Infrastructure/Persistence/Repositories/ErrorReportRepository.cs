using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;

namespace TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="IErrorReportRepository"/> EF uygulaması (genel <see cref="Repository{TEntity}"/> davranışı).
/// </summary>
public sealed class ErrorReportRepository : Repository<ErrorReport>, IErrorReportRepository
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">Diagnostics DbContext'i.</param>
    public ErrorReportRepository(DiagnosticsDbContext context)
        : base(context)
    {
    }
}
