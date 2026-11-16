using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Sabit katalog döndüren katalog servisi (B-89 testi).</summary>
/// <param name="catalog">Katalog.</param>
public sealed class StubCatalogService(CatalogDto catalog) : ICatalogService
{
    /// <inheritdoc />
    public Task<Result<CatalogDto>> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Result<CatalogDto>.Success(catalog));
}
