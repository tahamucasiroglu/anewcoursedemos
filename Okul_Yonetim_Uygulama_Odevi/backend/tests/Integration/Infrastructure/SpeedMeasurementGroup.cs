namespace OkulYonetim.Tests.Integration.Infrastructure;

/// <summary>
/// Hız ölçen testler (ör. spec 0010 AC-5: 40 öğrencide ≤ 1 sn) bu koleksiyonda: paralel çalışma kapalı, bütün paralel
/// testler bittikten sonra tek başına koşar. Ölçüm, aynı anda çalışan onlarca entegrasyon testinin yükünü değil uygulamanın
/// hızını gösterir (eşik gevşetilmez).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SpeedMeasurementGroup
{
    public const string Name = "Hız ölçümü";
}
