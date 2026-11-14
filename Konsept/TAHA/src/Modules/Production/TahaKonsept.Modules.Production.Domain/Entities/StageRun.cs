using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Domain.Entities;

/// <summary>
/// Bir işin tek aşama çalıştırması (<see cref="Job"/>'un alt varlığı; sahne bazlı aşamalarda sahne başına bir kayıt).
/// Önemli alanlar: <see cref="InputHash"/> (aşama girdileri + etkileyen ayarlar + üst aşama çıktı hash'leri, SHA-256),
/// <see cref="OutputHash"/>, <see cref="OutputJson"/>, <see cref="ArtifactsJson"/> (online üretilen dosyaların indirme bilgileri),
/// <see cref="CommandId"/> (ajan komutu), maliyet/token/süre, hata. Yeni sürümde <see cref="Reused"/> kaynak işten kopyalanır.
/// Geçişler: <see cref="Start"/>, <see cref="Dispatch"/>, <see cref="ReportProgress"/>, <see cref="Complete"/>, <see cref="ReuseFrom"/>,
/// <see cref="Fail"/>, <see cref="ResetPending"/>, <see cref="RequestRepair"/>, <see cref="Cancel"/>.
/// </summary>
public sealed class StageRun : EntityBase<Guid>
{
    /// <summary>EF Core için boş kurucu.</summary>
    private StageRun()
    {
    }

    /// <summary>İş.</summary>
    public Guid JobId { get; private set; }

    /// <summary>Hat sırası (sıralama için).</summary>
    public int Sequence { get; private set; }

    /// <summary>Aşama.</summary>
    public PipelineStage Stage { get; private set; }

    /// <summary>Sahne indeksi (tüm video için <c>null</c>).</summary>
    public int? SceneIndex { get; private set; }

    /// <summary>Çalıştığı yer.</summary>
    public StageLocation Location { get; private set; }

    /// <summary>Durum.</summary>
    public StageStatus Status { get; private set; } = StageStatus.Pending;

    /// <summary>Girdi hash'i.</summary>
    public string InputHash { get; private set; } = string.Empty;

    /// <summary>Çıktı hash'i.</summary>
    public string? OutputHash { get; private set; }

    /// <summary>Çıktı (JSON).</summary>
    public string? OutputJson { get; private set; }

    /// <summary>Online üretilmiş artefaktların indirme bilgileri (JSON dizi).</summary>
    public string? ArtifactsJson { get; private set; }

    /// <summary>Önceki sürümden yeniden kullanıldı mı.</summary>
    public bool Reused { get; private set; }

    /// <summary>Yeniden kullanılan kaynak çalıştırma.</summary>
    public Guid? ReusedFromRunId { get; private set; }

    /// <summary>Yeniden kullanım adayı (ajan aşaması aynı hash ile gönderildi; ajan çıktıyı atlayacak).</summary>
    public bool ReuseCandidate { get; private set; }

    /// <summary>Yeniden kullanım yasak mı (ör. eski artefakt indirilemedi).</summary>
    public bool ReuseDisabled { get; private set; }

    /// <summary>Ajan komut kimliği.</summary>
    public Guid? CommandId { get; private set; }

    /// <summary>İlerleme (0-100).</summary>
    public double Progress { get; private set; }

    /// <summary>Son mesaj.</summary>
    public string? Message { get; private set; }

    /// <summary>Deneme sayısı.</summary>
    public int Attempts { get; private set; }

    /// <summary>LLM'e hata ile yeniden yazdırma sayısı (sahne kodu).</summary>
    public int RepairCount { get; private set; }

    /// <summary>Yeniden yazım için doğrulama hatası (sahne kodu; boşsa normal üretim).</summary>
    public string? RepairHint { get; private set; }

    /// <summary>Giriş token'ı.</summary>
    public long InputTokens { get; private set; }

    /// <summary>Çıkış token'ı.</summary>
    public long OutputTokens { get; private set; }

    /// <summary>Maliyet ($).</summary>
    public decimal CostUsd { get; private set; }

    /// <summary>Başlama (UTC).</summary>
    public DateTimeOffset? StartedAtUtc { get; private set; }

    /// <summary>Ajana gönderilme (UTC).</summary>
    public DateTimeOffset? DispatchedAtUtc { get; private set; }

    /// <summary>Son ilerleme raporu (UTC).</summary>
    public DateTimeOffset? LastProgressAtUtc { get; private set; }

    /// <summary>Bitiş (UTC).</summary>
    public DateTimeOffset? FinishedAtUtc { get; private set; }

    /// <summary>Süre (ms).</summary>
    public long DurationMs { get; private set; }

    /// <summary>Hata kodu.</summary>
    public string? ErrorCode { get; private set; }

    /// <summary>Hata mesajı.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Bitti mi (tamamlandı ya da yeniden kullanıldı).</summary>
    public bool IsDone => Status is StageStatus.Completed or StageStatus.Skipped;

    /// <summary>Yeni bekleyen çalıştırma oluşturur.</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="sequence">Hat sırası.</param>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <param name="location">Varsayılan yer.</param>
    /// <returns>Çalıştırma.</returns>
    public static StageRun Create(Guid jobId, int sequence, PipelineStage stage, int? sceneIndex, StageLocation location) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            JobId = jobId,
            Sequence = sequence,
            Stage = stage,
            SceneIndex = sceneIndex,
            Location = location,
        };

    /// <summary>Sunucu aşamasını başlatır.</summary>
    /// <param name="inputHash">Girdi hash'i.</param>
    /// <param name="location">Yer.</param>
    /// <param name="nowUtc">Şimdi.</param>
    public void Start(string inputHash, StageLocation location, DateTimeOffset nowUtc)
    {
        InputHash = inputHash;
        Location = location;
        Status = StageStatus.Running;
        Attempts++;
        StartedAtUtc = nowUtc;
        FinishedAtUtc = null;
        Progress = 0;
        ErrorCode = null;
        ErrorMessage = null;
        Message = null;
    }

    /// <summary>Ajan aşamasını gönderilmiş olarak işaretler.</summary>
    /// <param name="inputHash">Girdi hash'i.</param>
    /// <param name="commandId">Komut.</param>
    /// <param name="reuseCandidate">Kaynak işte aynı hash bulundu mu.</param>
    /// <param name="nowUtc">Şimdi.</param>
    public void Dispatch(string inputHash, Guid commandId, bool reuseCandidate, DateTimeOffset nowUtc)
    {
        Start(inputHash, StageLocation.Agent, nowUtc);
        CommandId = commandId;
        ReuseCandidate = reuseCandidate;
        DispatchedAtUtc = nowUtc;
        LastProgressAtUtc = nowUtc;
        Message = "Masaüstü programına gönderildi";
    }

    /// <summary>İlerleme kaydeder.</summary>
    /// <param name="percent">Yüzde.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="nowUtc">Şimdi.</param>
    public void ReportProgress(double percent, string? message, DateTimeOffset nowUtc)
    {
        Progress = Math.Clamp(percent, 0, 100);
        if (!string.IsNullOrWhiteSpace(message))
        {
            Message = ProductionLimits.Clip(message, ProductionLimits.MessageMaxLength);
        }

        LastProgressAtUtc = nowUtc;
    }

    /// <summary>Tamamlar.</summary>
    /// <param name="outputJson">Çıktı JSON'u.</param>
    /// <param name="outputHash">Çıktı hash'i.</param>
    /// <param name="artifactsJson">Artefakt bilgileri.</param>
    /// <param name="costUsd">Maliyet.</param>
    /// <param name="inputTokens">Giriş token'ı.</param>
    /// <param name="outputTokens">Çıkış token'ı.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="nowUtc">Şimdi.</param>
    /// <param name="durationMs">Süre (verilmezse başlangıçtan hesaplanır).</param>
    public void Complete(
        string? outputJson,
        string outputHash,
        string? artifactsJson,
        decimal costUsd,
        long inputTokens,
        long outputTokens,
        string? message,
        DateTimeOffset nowUtc,
        long? durationMs = null)
    {
        OutputJson = outputJson;
        OutputHash = outputHash;
        ArtifactsJson = artifactsJson;
        CostUsd += costUsd;
        InputTokens += inputTokens;
        OutputTokens += outputTokens;
        Progress = 100;
        Message = ProductionLimits.ClipOrNull(message, ProductionLimits.MessageMaxLength);
        FinishedAtUtc = nowUtc;
        DurationMs = durationMs ?? (StartedAtUtc is null ? 0 : (long)(nowUtc - StartedAtUtc.Value).TotalMilliseconds);
        Reused = ReuseCandidate;
        Status = ReuseCandidate ? StageStatus.Skipped : StageStatus.Completed;
        RepairHint = null;
        ErrorCode = null;
        ErrorMessage = null;
    }

    /// <summary>Kaynak işin çalıştırmasını yeniden kullanır (çıktı kopyalanır, maliyet 0).</summary>
    /// <param name="source">Kaynak çalıştırma.</param>
    /// <param name="nowUtc">Şimdi.</param>
    public void ReuseFrom(StageRun source, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(source);
        InputHash = source.InputHash;
        OutputHash = source.OutputHash;
        OutputJson = source.OutputJson;
        ArtifactsJson = source.ArtifactsJson;
        Location = source.Location;
        Reused = true;
        ReusedFromRunId = source.Id;
        Status = StageStatus.Skipped;
        Progress = 100;
        Message = "Önceki sürümden yeniden kullanıldı";
        StartedAtUtc = nowUtc;
        FinishedAtUtc = nowUtc;
        DurationMs = 0;
    }

    /// <summary>Hata ile bitirir.</summary>
    /// <param name="code">Hata kodu.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="nowUtc">Şimdi.</param>
    public void Fail(string code, string message, DateTimeOffset nowUtc)
    {
        Status = StageStatus.Failed;
        ErrorCode = ProductionLimits.Clip(code, ProductionLimits.ErrorCodeMaxLength);
        ErrorMessage = ProductionLimits.Clip(message, ProductionLimits.MessageMaxLength);
        Message = ErrorMessage;
        FinishedAtUtc = nowUtc;
        DurationMs = StartedAtUtc is null ? 0 : (long)(nowUtc - StartedAtUtc.Value).TotalMilliseconds;
    }

    /// <summary>Beklemeye döndürür (duraklatma, geçici hata, yeniden deneme).</summary>
    /// <param name="message">Mesaj.</param>
    public void ResetPending(string? message)
    {
        Status = StageStatus.Pending;
        CommandId = null;
        Progress = 0;
        Message = ProductionLimits.ClipOrNull(message, ProductionLimits.MessageMaxLength);
    }

    /// <summary>Yeniden kullanımı kapatır ve beklemeye döndürür.</summary>
    /// <param name="message">Mesaj.</param>
    public void DisableReuse(string? message)
    {
        ReuseDisabled = true;
        Reused = false;
        ReuseCandidate = false;
        ReusedFromRunId = null;
        ResetPending(message);
    }

    /// <summary>Sahne kodunu doğrulama hatasıyla yeniden yazdırmak için beklemeye alır.</summary>
    /// <param name="validationError">Ajanın doğrulama hatası.</param>
    public void RequestRepair(string validationError)
    {
        RepairCount++;
        RepairHint = ProductionLimits.Clip(validationError, ProductionLimits.MessageMaxLength);
        Reused = false;
        ReuseCandidate = false;
        ResetPending("Sahne kodu doğrulama hatasıyla yeniden yazılacak");
    }

    /// <summary>İptal eder.</summary>
    /// <param name="nowUtc">Şimdi.</param>
    public void Cancel(DateTimeOffset nowUtc)
    {
        if (!IsDone)
        {
            Status = StageStatus.Cancelled;
            FinishedAtUtc = nowUtc;
            Message = "İptal edildi";
        }
    }
}
