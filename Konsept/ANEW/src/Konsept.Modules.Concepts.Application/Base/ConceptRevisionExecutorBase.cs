using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.AiGateway.Contracts.Enums;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Application.Resources;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Talimatla yeniden üretimin yürütücüsü (spec 0015 Req 7, 10, 12; H-3). Kurallar:
/// <list type="bullet">
/// <item>Çağrı <see cref="IModelCaller"/> ile 0013 kapısından konsept üretimi rolü ve konseptin profiliyle geçer (harcama kaydı, BR-12);
/// bu sınıf hiçbir çağrıyı kendisi tekrar etmez.</item>
/// <item>Sistem talimatı 0014'ün konsept talimatıdır (profil, ince ayar, doğruluk); mevcut konsept ve düzenleme talimatı istemde veri
/// bloklarındadır.</item>
/// <item>Yarım kalan, ayrıştırılamayan çıktı ya da sağlayıcı hatası revizyon olmaz: sade hata, otomatik tekrar yok.</item>
/// <item>Kelime denetimi açıkken eşik aşılırsa en fazla 1 yeniden üretim (BR-15); yine aşılırsa (ya da ikinci çıktı kullanılamazsa)
/// revizyon "kelime tekrarı" uyarısıyla gösterilir.</item>
/// <item>Başlık değişmez; revizyon yalnızca sahnelerdir.</item>
/// </list>
/// </summary>
/// <param name="caller">Model çağrısı (AiGateway.Contracts).</param>
/// <param name="rules">Kurallar.</param>
/// <param name="logger">Log.</param>
public abstract class ConceptRevisionExecutorBase(IModelCaller caller, IConceptRuleSource rules, ILogger logger) : IConceptRevisionExecutor
{
    private static readonly Action<ILogger, Guid, Exception?> RevisionCrashed = LoggerMessage.Define<Guid>(
        LogLevel.Error, new EventId(5111, "ConceptRevisionCrashed"), "Konsept yeniden üretimi beklenmeyen hatayla durdu: {RevisionId}");

    /// <inheritdoc />
    public async Task ExecuteAsync(ConceptRevision revision, RevisionInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(revision);
        ArgumentNullException.ThrowIfNull(input);
        try
        {
            await RunAsync(revision, input, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                RevisionCrashed(logger, revision.Id, ex);
            }

            revision.Fail(ConceptsText.RevisionUnexpected);
        }
    }

    private async Task RunAsync(ConceptRevision revision, RevisionInput input, CancellationToken cancellationToken)
    {
        var context = input.Context;
        var concept = input.Concept;
        var system = ConceptPrompts.ConceptSystem(
            context.Instruction, new FineTuneChoice(context.Tone, context.Audience, context.Narration), concept.TargetDurationSeconds);
        string Prompt(bool avoid) => ConceptPrompts.RevisionPrompt(concept.Title, input.BaseScenes, input.Instruction, avoid);

        var (first, error) = await OnceAsync(input, system, Prompt(false), cancellationToken).ConfigureAwait(false);
        if (first is null)
        {
            revision.Fail(error!);
            return;
        }

        var scenes = first;
        var warning = false;
        if (context.WordCheck && Repeats(first, concept.Language))
        {
            // BR-15: en fazla 1 yeniden üretim; ikinci çıktı kullanılamazsa ilk revizyon uyarıyla gösterilir.
            var (second, _) = await OnceAsync(input, system, Prompt(true), cancellationToken).ConfigureAwait(false);
            scenes = second ?? first;
            warning = second is null || Repeats(second, concept.Language);
        }

        var mismatch = DurationFit.IsMismatch(scenes.Sum(s => s.DurationSeconds), concept.TargetDurationSeconds, rules.Rules.DurationTolerance);
        revision.Complete(scenes, warning, mismatch);
    }

    private async Task<(IReadOnlyList<ConceptScene>? Scenes, string? Error)> OnceAsync(
        RevisionInput input, string system, string prompt, CancellationToken cancellationToken)
    {
        var context = input.Context;
        var reply = await caller.CallTextAsync(
            new TextModelCall
            {
                Caller = input.Caller,
                ModelId = context.ConceptModel!,
                Role = ModelRole.ConceptGeneration,
                ProfileId = context.ProfileId,
                Effort = context.ConceptEffort,
                System = system,
                Prompt = prompt,
            },
            cancellationToken).ConfigureAwait(false);
        if (!reply.IsSuccess)
        {
            return (null, reply.Error!.Message);
        }

        if (reply.Value.Truncated)
        {
            return (null, ConceptsText.ConceptTruncated);
        }

        return ModelOutputParser.Concept(reply.Value.Text, allowEmptyVisual: true) is { } parsed
            ? (parsed.Scenes, null)
            : (null, ConceptsText.ConceptMalformed);
    }

    private bool Repeats(IReadOnlyList<ConceptScene> scenes, string language) =>
        WordRepetition.Exceeds(scenes.Select(s => s.Narration), language, rules.Rules);
}
