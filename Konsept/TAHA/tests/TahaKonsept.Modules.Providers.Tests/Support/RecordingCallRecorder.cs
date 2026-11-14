using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Concrete;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// Sahte çağrı kaydedici: kayıtları bellekte tutar, maliyeti gerçek <see cref="CostCalculator"/> ile katalog fiyatından hesaplar.
/// </summary>
public sealed class RecordingCallRecorder : ICallRecorder
{
    private readonly CostCalculator _calculator = new(new FixedClock());

    /// <summary>Kaydedilen çağrılar.</summary>
    public List<CallRecordRequest> Records { get; } = [];

    /// <inheritdoc />
    public Task<decimal> RecordAsync(CallRecordRequest request, CancellationToken ct)
    {
        Records.Add(request);
        return Task.FromResult(_calculator.Calculate(request.Model.Pricing, request.Usage));
    }
}
