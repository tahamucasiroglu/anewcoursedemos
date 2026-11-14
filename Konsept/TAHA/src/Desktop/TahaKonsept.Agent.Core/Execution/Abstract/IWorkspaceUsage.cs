namespace TahaKonsept.Agent.Core.Execution.Abstract;

/// <summary>
/// Hangi çalışma alanlarının (iş klasörü, <c>Workspace/&lt;jobId&gt;</c>) şu an kuyrukta ya da çalışan bir aşama tarafından kullanıldığını izler.
/// İş çalıştırıcı aşama kuyruğa girince <see cref="Enter"/>, bitince <see cref="Exit"/> çağırır; video silme gibi temizlikler
/// <see cref="IsInUse"/> ile paylaşılan alanı (yeni sürüm aynı alanı kullanır) silmekten kaçınır. Bağımlılığı yoktur (döngüsel DI olmaz).
/// </summary>
public interface IWorkspaceUsage
{
    /// <summary>Alanı kullanmaya başlar (sayaç +1).</summary>
    /// <param name="workspaceId">Çalışma alanı (komutun <c>JobId</c>'si).</param>
    void Enter(Guid workspaceId);

    /// <summary>Kullanımı bırakır (sayaç -1).</summary>
    /// <param name="workspaceId">Çalışma alanı.</param>
    void Exit(Guid workspaceId);

    /// <summary>Alan kuyrukta/çalışan bir aşamada kullanılıyor mu.</summary>
    /// <param name="workspaceId">Çalışma alanı.</param>
    /// <returns>Sonuç.</returns>
    bool IsInUse(Guid workspaceId);
}
