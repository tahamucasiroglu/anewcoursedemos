using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Monitoring.Presentation.Controllers;

/// <summary>
/// Günlük geçmişi (spec 0021 Req 5–8): giriş ister (default-deny); kullanıcı yalnızca kendi kayıtlarını görür.
/// </summary>
[ApiController]
public sealed class JournalController : ControllerBase
{
    private readonly IJournalService _journal;

    /// <summary>Initializes a new instance of the <see cref="JournalController"/> class.</summary>
    /// <param name="journal">Günlük servisi.</param>
    public JournalController(IJournalService journal)
    {
        _journal = journal;
    }

    /// <summary>Bir günün kayıtları (en yenisi üstte, 50'şer).</summary>
    /// <param name="request">Gün, filtre, atlanacak kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gün sayfası.</returns>
    [HttpGet("/api/journal")]
    public Task<IActionResult> Day([FromQuery] JournalDayRequest request, CancellationToken cancellationToken) =>
        _journal.GetDayAsync(request, cancellationToken).ToActionResult();
}
