using System.Collections.Concurrent;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0012 AC-8 (VERIFY V-1): sesli not isteği süresince sistem geçici klasörünü (<see cref="Path.GetTempPath"/>) alt
/// klasörleriyle birlikte izler ve sesin diske düştüğünü gösteren her dosyayı yakalar — yalnızca ASP.NET form tamponu değil,
/// uygulamanın kendisinin yazdığı dosya da. Şüpheli sayılan dosya: (1) adı ASP.NET tamponu (<c>ASPNETCORE_*</c>) ya da ses
/// uzantılı (<c>.m4a</c>, <c>.wav</c> …) olan, ya da (2) içeriğinde bu teste özgü işaret baytları geçen her yeni/değişen dosya.
/// İşaret her örnekte yeni bir <see cref="Guid"/>'dir ve sesin tamamı bu işaretin tekrarıdır — sesin herhangi bir parçası
/// (en az 32 bayt) yazılsa yakalanır. Paralel koşan diğer testler kendi (farklı) seslerini gönderdiği ve makinedeki başka
/// süreçlerin dosyaları bu işareti taşımadığı için gürültü yanlış alarm üretmez; ad kuralına takılan dosya ise hangi test
/// tetiklemiş olursa olsun gerçek bir sızıntıdır. Bu yüzden ayrı, paralel olmayan bir koleksiyon gerekmez.
/// Sınır: yazılıp okunamadan silinen, adı nötr bir dosya içerikten doğrulanamaz — o yol mimari testle
/// (<c>AiModuleRulesTests.VoiceNotePath_DoesNotWriteFiles</c>) kapatılır.
/// </summary>
internal sealed class TempDirectoryAudioWatch : IDisposable
{
    private const long MaxInspectBytes = 32L * 1024 * 1024;

    private static readonly string[] AudioExtensions = [".m4a", ".mp4", ".aac", ".wav", ".webm", ".ogg", ".oga", ".opus", ".mp3", ".3gp", ".caf", ".amr", ".flac"];

    private readonly FileSystemWatcher watcher;
    private readonly ConcurrentDictionary<string, byte> seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> suspicious = new(StringComparer.OrdinalIgnoreCase);
    private readonly byte[] marker;

    private TempDirectoryAudioWatch(byte[] marker)
    {
        this.marker = marker;
        this.watcher = new FileSystemWatcher(Path.GetTempPath())
        {
            Filter = "*",
            IncludeSubdirectories = true,
            InternalBufferSize = 64 * 1024,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
        };
        this.watcher.Created += (_, e) => this.Inspect(e.FullPath);
        this.watcher.Changed += (_, e) => this.Inspect(e.FullPath);
        this.watcher.Renamed += (_, e) => this.Inspect(e.FullPath);
        this.watcher.Error += (_, e) => this.suspicious.TryAdd("<izleyici>", "izleyici olay kaçırdı: " + e.GetException().Message);
        this.watcher.EnableRaisingEvents = true;
    }

    /// <summary>Bu izlemeye özgü, işaretin tekrarından oluşan ses baytları.</summary>
    public byte[] Audio(int length)
    {
        byte[] audio = new byte[length];
        for (int i = 0; i < audio.Length; i++)
        {
            audio[i] = this.marker[i % this.marker.Length];
        }

        return audio;
    }

    /// <summary>Geçici klasörde izlemeyi başlatır; işaret her çağrıda yenidir.</summary>
    public static TempDirectoryAudioWatch Start() => new([.. Guid.NewGuid().ToByteArray(), .. Guid.NewGuid().ToByteArray()]);

    /// <summary>
    /// İzlemeyi durdurur, görülen dosyaları son bir kez denetler ve şüpheli bulunanları "yol: neden" olarak döndürür.
    /// </summary>
    public IReadOnlyList<string> Stop()
    {
        this.watcher.EnableRaisingEvents = false;
        foreach (string path in this.seen.Keys)
        {
            this.Inspect(path);
        }

        return [.. this.suspicious.Select(p => $"{p.Key}: {p.Value}")];
    }

    public void Dispose() => this.watcher.Dispose();

    private void Inspect(string path)
    {
        this.seen.TryAdd(path, 0);
        string name = Path.GetFileName(path);
        if (name.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase))
        {
            this.suspicious.TryAdd(path, "ASP.NET form tamponu");
            return;
        }

        if (AudioExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
        {
            this.suspicious.TryAdd(path, "ses uzantılı dosya");
            return;
        }

        if (this.ContainsMarker(path))
        {
            this.suspicious.TryAdd(path, "içerikte sesin baytları");
        }
    }

    private bool ContainsMarker(string path)
    {
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length < this.marker.Length || stream.Length > MaxInspectBytes)
            {
                return false;
            }

            byte[] content = new byte[stream.Length];
            stream.ReadExactly(content);
            return content.AsSpan().IndexOf(this.marker) >= 0 || this.ContainsShiftedMarker(content);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    // Ses parçası işaretin ortasından başlayabilir: işaretin her dönüşü (rotasyonu) için ara.
    private bool ContainsShiftedMarker(byte[] content)
    {
        byte[] rotated = new byte[this.marker.Length];
        for (int shift = 1; shift < this.marker.Length; shift++)
        {
            for (int i = 0; i < rotated.Length; i++)
            {
                rotated[i] = this.marker[(i + shift) % this.marker.Length];
            }

            if (content.AsSpan().IndexOf(rotated) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
