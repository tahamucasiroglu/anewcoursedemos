using TahaKonsept.Agent.Core.Storage.Base;
using TahaKonsept.Agent.Settings.Abstract;

namespace TahaKonsept.Agent.Settings.Base;

/// <summary>
/// JSON dosyasında saklanan <see cref="IDesktopAppSettingsStore"/> tabanı: ilk okumada bellekte önbelleğe alır,
/// <see cref="Update"/> kilit altında değiştirip atomik yazar (çekirdeğin <c>JsonFileStoreBase</c>'i).
/// </summary>
public abstract class DesktopAppSettingsStoreBase : JsonFileStoreBase<DesktopAppSettings>, IDesktopAppSettingsStore
{
    private DesktopAppSettings? _current;

    /// <summary>Oluşturur.</summary>
    /// <param name="filePath">Ayar dosyası yolu.</param>
    protected DesktopAppSettingsStoreBase(string filePath)
        : base(filePath)
    {
    }

    /// <inheritdoc />
    public DesktopAppSettings Current
    {
        get
        {
            lock (Gate)
            {
                return _current ??= Read() ?? new DesktopAppSettings();
            }
        }
    }

    /// <inheritdoc />
    public DesktopAppSettings Update(Func<DesktopAppSettings, DesktopAppSettings> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        lock (Gate)
        {
            var next = change(Current);
            Write(next);
            _current = next;
            return next;
        }
    }
}
