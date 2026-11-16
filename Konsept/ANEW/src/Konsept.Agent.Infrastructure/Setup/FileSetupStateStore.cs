using System.Text.Json;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Infrastructure.Setup;

/// <summary>Kurulum durumu dosyası: <c>%LOCALAPPDATA%\anewconcept\setup-state.json</c> (spec 0009 Req 15, 19–20).</summary>
/// <param name="directory">Kurulum yeri.</param>
public sealed class FileSetupStateStore(string directory) : ISetupStateStore
{
    /// <summary>Dosya adı.</summary>
    public const string FileName = "setup-state.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private string FilePath => Path.Combine(directory, FileName);

    /// <inheritdoc />
    public SetupState Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var state = JsonSerializer.Deserialize<SetupState>(File.ReadAllText(FilePath), Json);
                if (state?.Installed is not null)
                {
                    return state;
                }
            }
        }
        catch (JsonException)
        {
            // Bozuk durum dosyası: bileşenler yeniden doğrulanır/kurulur (kurulu dosyalar hash'le yeniden tanınmaz, güvenli taraf).
        }

        return new SetupState(new Dictionary<string, InstalledComponent>(StringComparer.Ordinal), null);
    }

    /// <inheritdoc />
    public void Save(SetupState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Directory.CreateDirectory(directory);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(state, Json));
        File.Move(temp, FilePath, overwrite: true);
    }
}
