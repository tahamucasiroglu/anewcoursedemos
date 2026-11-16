using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>İşi istekten bağımsız arka planda başlatır (tarayıcı kapansa da sürer; spec 0014 Req 20).</summary>
public interface IConceptJobLauncher
{
    /// <summary>Başlatır ve hemen döner.</summary>
    /// <param name="job">İş.</param>
    /// <param name="input">Girdi.</param>
    void Launch(ConceptJob job, ConceptJobInput input);
}
