using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Konsept.Modules.Concepts.Infrastructure.Jobs.Base;

/// <summary>
/// İşi istekten bağımsız bir görevde başlatır (spec 0014 Req 20): istek kapsamı değil kendi DI kapsamını açar, böylece tarayıcı kapansa
/// da sürer. Uygulama kapanırken işe iptal iletilir (iş durdurulmuş sayılır). Tekil (singleton) kaydedilir.
/// </summary>
/// <param name="scopes">Kapsam fabrikası.</param>
/// <param name="lifetime">Uygulama yaşam döngüsü.</param>
public abstract class ConceptJobLauncherBase(IServiceScopeFactory scopes, IHostApplicationLifetime lifetime) : IConceptJobLauncher
{
    /// <inheritdoc />
    public void Launch(ConceptJob job, ConceptJobInput input)
    {
        var stopping = lifetime.ApplicationStopping;
        _ = Task.Run(
            async () =>
            {
                await using var scope = scopes.CreateAsyncScope();
                var executor = scope.ServiceProvider.GetRequiredService<IConceptJobExecutor>();
                await executor.ExecuteAsync(job, input, stopping).ConfigureAwait(false);
            },
            CancellationToken.None);
    }
}
