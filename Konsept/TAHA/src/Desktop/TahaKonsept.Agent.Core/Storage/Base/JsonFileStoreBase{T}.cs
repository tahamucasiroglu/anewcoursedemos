using System.Text.Json;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Agent.Core.Storage.Base;

/// <summary>
/// Tek bir JSON dosyasında değer saklayan iş parçacığı güvenli taban. Yazma atomiktir (geçici dosya + taşıma);
/// bozuk ya da okunamayan dosya <c>null</c> sayılır. Ayarlar ve video dizini bunu kullanır.
/// Önemli üyeler: <see cref="Read"/>, <see cref="Write"/>, <see cref="Gate"/>.
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

    /// <summary>Dosyayı okur; yoksa ya da bozuksa <c>null</c>.</summary>
    /// <returns>Değer ya da <c>null</c>.</returns>
    protected T? Read()
    {
        lock (Gate)
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(FilePath);
                return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json, FileJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
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
