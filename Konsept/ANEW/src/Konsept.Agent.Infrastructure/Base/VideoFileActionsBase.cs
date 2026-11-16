using Konsept.Agent.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Dosya eylemlerinin ortak mantığı (spec 0007 Req 4, 9): yalnızca video klasöründeki düz dosya adı (yol, <c>..</c> yok) ve yalnızca iki eylem
/// kabul edilir; dosya yoksa <see cref="AgentFileActionReply.NotFound"/>. Kabuk çağrısı alt sınıftadır; hata verirse
/// <see cref="AgentFileActionReply.Failed"/>.
/// </summary>
/// <param name="videoFolder">Video klasörünü veren kaynak.</param>
public abstract class VideoFileActionsBase(Func<string> videoFolder) : IVideoFileActions
{
    /// <inheritdoc />
    public AgentFileActionReply Open(AgentFileActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Action is not (AgentFileActionRequest.Folder or AgentFileActionRequest.Player))
        {
            return new AgentFileActionReply(false, AgentFileActionReply.Failed);
        }

        var name = request.FileName;
        if (string.IsNullOrWhiteSpace(name) || !string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal))
        {
            return new AgentFileActionReply(false, AgentFileActionReply.NotFound);
        }

        var path = Path.GetFullPath(Path.Combine(videoFolder(), name));
        if (!File.Exists(path))
        {
            return new AgentFileActionReply(false, AgentFileActionReply.NotFound);
        }

        try
        {
            if (request.Action == AgentFileActionRequest.Folder)
            {
                RevealInFolder(path);
            }
            else
            {
                OpenWithDefaultApp(path);
            }

            return new AgentFileActionReply(true, null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return new AgentFileActionReply(false, AgentFileActionReply.Failed);
        }
    }

    /// <summary>Dosya gezgininde dosyanın klasörünü, dosya seçili olarak açar.</summary>
    /// <param name="path">Tam yol (doğrulanmış, var).</param>
    protected abstract void RevealInFolder(string path);

    /// <summary>Dosyayı varsayılan uygulamada açar.</summary>
    /// <param name="path">Tam yol (doğrulanmış, var).</param>
    protected abstract void OpenWithDefaultApp(string path);
}
