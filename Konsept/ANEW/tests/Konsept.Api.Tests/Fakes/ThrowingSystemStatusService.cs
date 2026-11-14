using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Beklenmeyen hata üreten durum servisi: ProblemDetails + korelasyon numarası yolunu sınamak için.</summary>
public sealed class ThrowingSystemStatusService : ISystemStatusService
{
    /// <summary>Hata mesajı (gizli bir key de içerir; logda maskeli görünmelidir).</summary>
    public const string Message = "beklenmeyen test hatası sk-proj-THROWINGKEY123-t9z";

    /// <inheritdoc />
    public Task<Result<SystemStatusDto>> GetStatusAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);

    /// <inheritdoc />
    public Task<Result<HealthDto>> GetHealthAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);
}
