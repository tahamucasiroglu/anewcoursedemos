using Konsept.Modules.Concepts.Infrastructure.Jobs.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Konsept.Modules.Concepts.Infrastructure.Jobs.Concrete;

/// <summary>Arka plan iş başlatıcı; davranış <see cref="ConceptJobLauncherBase"/>'tedir.</summary>
/// <param name="scopes">Kapsam fabrikası.</param>
/// <param name="lifetime">Uygulama yaşam döngüsü.</param>
public sealed class ConceptJobLauncher(IServiceScopeFactory scopes, IHostApplicationLifetime lifetime) : ConceptJobLauncherBase(scopes, lifetime);
