using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;

namespace TahaKonsept.Modules.Diagnostics.Application.Abstract;

/// <summary>
/// <see cref="ErrorReport"/> deposu (genel <see cref="IRepository{TEntity}"/> yeterli; özel sorgu gerekirse buraya eklenir).
/// </summary>
public interface IErrorReportRepository : IRepository<ErrorReport>;
