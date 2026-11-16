using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Talimatla yeniden üretimin yürütücüsü; davranış <see cref="ConceptRevisionExecutorBase"/>'tedir.</summary>
/// <param name="caller">Model çağrısı.</param>
/// <param name="rules">Kurallar.</param>
/// <param name="logger">Log.</param>
public sealed class ConceptRevisionExecutor(IModelCaller caller, IConceptRuleSource rules, ILogger<ConceptRevisionExecutor> logger)
    : ConceptRevisionExecutorBase(caller, rules, logger);
