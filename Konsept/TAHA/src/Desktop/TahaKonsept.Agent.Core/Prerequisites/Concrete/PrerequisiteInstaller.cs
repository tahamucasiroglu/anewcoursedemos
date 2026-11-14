using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Concrete;

/// <summary>DI'daki tüm <see cref="IPrerequisite"/> kayıtlarını kuran, sonuçları loglayan yönetici.</summary>
public sealed class PrerequisiteInstaller : PrerequisiteInstallerBase
{
    private readonly ILogger<PrerequisiteInstaller> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="prerequisites">Bileşenler.</param>
    /// <param name="logger">Logger.</param>
    public PrerequisiteInstaller(IEnumerable<IPrerequisite> prerequisites, ILogger<PrerequisiteInstaller> logger)
        : base(prerequisites)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override void OnStepCompleted(IPrerequisite prerequisite, Result result)
    {
        if (result.IsSuccess)
        {
            _logger.LogInformation("Bileşen hazır: {Id} ({Version})", prerequisite.Id, prerequisite.InstalledVersion);
        }
        else
        {
            _logger.LogError("Bileşen kurulamadı: {Id} → {Code}: {Message}", prerequisite.Id, result.Error.Code, result.Error.Message);
        }
    }
}
