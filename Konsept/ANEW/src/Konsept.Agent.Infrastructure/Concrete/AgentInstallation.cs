using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>Ajanın kurulu kopyası (spec 0023 Req 9–11, B-77); davranış <see cref="AgentInstallationBase"/>'dedir.</summary>
/// <param name="installRoot">Kurulum yeri (<c>%LOCALAPPDATA%\anewconcept</c>).</param>
/// <param name="startMenuDirectory">Kullanıcının Başlat menüsü "Programlar" klasörü.</param>
/// <param name="host">Kayıt defteri (yalnızca HKCU), kısayol, süreç.</param>
public sealed class AgentInstallation(string installRoot, string startMenuDirectory, IInstallHost host)
    : AgentInstallationBase(installRoot, startMenuDirectory, host);
