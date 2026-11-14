using System.Text.Json;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Agent.Core.Storage.Base;

/// <summary>
/// Tek bir JSON dosyasında değer saklayan iş parçacığı güvenli taban. Yazma atomiktir (geçici dosya + taşıma).
/// "Dosya yok" ile "okunamadı" ayrılır (<see cref="TryRead"/>): geçici kilit (antivirüs, yedekleme) kısa aralıklarla yeniden denenir, yine
/// okunamazsa başarısız sayılır (çağıran boş değerle üzerine yazmasın); bozuk JSON <c>.bad</c> uzantılı yedeğe taşınıp boş başlanır.
/// Ayarlar ve video dizini bunu kullanır. Önemli üyeler: <see cref="Read"/>, <see cref="TryRead"/>, <see cref="Write"/>, <see cref="Gate"/>.
/// </summary>
/// <typeparam name="T">Saklanan tür.</typeparam>
public abstract class JsonFileStoreBase<T>
    where T : class
{
    private static readonly JsonSerializerOptions FileJsonOptions = new(ContractJson.Options) { WriteIndented = true };

    /// <summary>Oluşturur.</summary>
    /// <param name="filePath">JSON dosyasının tam yolu.</param>
    protected JsonFileStoreBase(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    /// <summary>JSON dosyasının yolu.</summary>
    protected string FilePath { get; }

    /// <summary>Okuma/yazma kilidi (türetilen sınıflar bellek içi durumu korumak için de kullanır).</summary>
    protected object Gate { get; } = new();

    /// <summary>Okuma kilitliyken en fazla deneme.</summary>
    protected virtual int ReadAttempts => 5;

    /// <summary>Okuma denemeleri arası bekleme.</summary>
    protected virtual TimeSpan ReadRetryDelay => TimeSpan.FromMilliseconds(100);

    /// <summary>Dosyayı okur; yoksa, bozuksa ya da okunamıyorsa <c>null</c> (yalnız okunan değerin kaybı önemsizse kullanın).</summary>
    /// <returns>Değer ya da <c>null</c>.</returns>
    protected T? Read() => TryRead(out var value) ? value : null;

    /// <summary>
    /// Dosyayı okur. Dosya yoksa <c>true</c> + <c>null</c>; bozuk JSON yedeklenir, <c>true</c> + <c>null</c>; dosya kilitli/okunamıyorsa
    /// yeniden denemelerden sonra <c>false</c> (çağıran boş değeri önbelleğe almamalı ve üzerine yazmamalı).
    /// </summary>
    /// <param name="value">Okunan değer.</param>
    /// <returns>Okuma sonuçlandıysa <c>true</c>.</returns>
    protected bool TryRead(out T? value)
    {
        lock (Gate)
        {
            value = null;
            for (var attempt = 1; ; attempt++)
            {
                if (!File.Exists(FilePath))
                {
                    return true;
                }

                string json;
                try
                {
                    json = File.ReadAllText(FilePath);
                }
                catch (IOException) when (attempt < ReadAttempts)
                {
                    Thread.Sleep(ReadRetryDelay);
                    continue;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return false;
                }

                try
                {
                    value = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json, FileJsonOptions);
                    return true;
                }
                catch (JsonException)
                {
                    BackUpCorruptFile();
                    return true;
                }
            }
        }
    }

    /// <summary>Bozuk dosyayı <c>&lt;ad&gt;.&lt;zaman&gt;.bad</c> olarak kenara taşır (içerik elle kurtarılabilsin; sonraki yazma onu ezmesin).</summary>
    private void BackUpCorruptFile()
    {
        try
        {
            File.Move(FilePath, FilePath + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture) + ".bad", overwrite: true);
        }
        catch (IOException)
        {
            // Taşınamadı: bir sonraki okumada yeniden denenir.
        }
    }

    /// <summary>Değeri atomik olarak yazar.</summary>
    /// <param name="value">Değer.</param>
    protected void Write(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        lock (Gate)
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(value, FileJsonOptions));
            File.Move(temp, FilePath, overwrite: true);
        }
    }
}
