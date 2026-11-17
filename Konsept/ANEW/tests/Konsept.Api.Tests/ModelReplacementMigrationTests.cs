using Konsept.Modules.Production.Infrastructure.Persistence;
using Konsept.Modules.Profiles.Infrastructure.Persistence;
using Konsept.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Spec 0026 AC-4, AC-5: katalogdan kaldırılan model kimlikleri kayıtlı konsept ve üretim ayarlarında açılış migration'ıyla yerine
/// geçen kimliğe taşınır; efor ve diğer değerler değişmez; eski kimliği olmayan satır değişmez. Eşlemenin katalogla aynılığı
/// <see cref="AiCatalogTests.ReplacedModels_PointToCurrentSameKindEntries"/>.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ModelReplacementMigrationTests
{
    private const string PreviousProfiles = "20261116140917_AddConceptSettings";
    private const string PreviousProduction = "20261117082350_AddVersionJobs";

    /// <summary>AC-4, AC-5: eski kimlikli satırlar yeni kimliğe geçer; diğer satır ve değerler aynı kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Migration_ReplacesRetiredIds_InSettings()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = new PostgresTestDatabase();
        await database.InitializeAsync();
        await using var profiles = new ProfilesDbContext(new DbContextOptionsBuilder<ProfilesDbContext>()
            .UseNpgsql(database.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", ProfilesDbContext.SchemaName)).Options);
        await using var production = new ProductionDbContext(new DbContextOptionsBuilder<ProductionDbContext>()
            .UseNpgsql(database.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", ProductionDbContext.SchemaName)).Options);
        await profiles.GetService<IMigrator>().MigrateAsync(PreviousProfiles, ct);
        await production.GetService<IMigrator>().MigrateAsync(PreviousProduction, ct);

        var old = Guid.NewGuid();
        var current = Guid.NewGuid();
        foreach (var id in new[] { old, current })
        {
            await database.ScalarAsync(
                "INSERT INTO profiles.profiles (id, user_id, name, name_key, language, format, duration_seconds, content, created_at, updated_at) " +
                $"VALUES ('{id}', '{id}', 'P', 'p-{id}', 'tr', 'landscape', 60, 'içerik', now(), now())");
        }

        await database.ScalarAsync(
            "INSERT INTO profiles.concept_settings (profile_id, user_id, concept_model, concept_effort, dedupe_model, dedupe_effort, topic_model, topic_effort, tone, word_check, updated_at) " +
            $"VALUES ('{old}', '{old}', 'anthropic/claude-opus-4-5', 'high', 'anthropic/claude-haiku-4-5', NULL, 'openai/gpt-5-mini', 'low', 'samimi', true, now())");
        await database.ScalarAsync(
            "INSERT INTO profiles.concept_settings (profile_id, user_id, concept_model, concept_effort, dedupe_model, dedupe_effort, topic_model, topic_effort, word_check, updated_at) " +
            $"VALUES ('{current}', '{current}', 'glm/glm-5.3', NULL, NULL, NULL, 'anthropic/claude-sonnet-5-5', 'medium', false, now())");
        await database.ScalarAsync(
            "INSERT INTO production.settings (profile_id, user_id, subtitles, progress_bar, code_model, code_effort, engine, image_model, audio_model, updated_at) " +
            $"VALUES ('{old}', '{old}', true, false, 'openai/gpt-5', 'medium', 'local/hyperframes', 'openai/gpt-image-1', 'local/supertonic-3', now())");
        await database.ScalarAsync(
            "INSERT INTO production.settings (profile_id, user_id, subtitles, progress_bar, code_model, code_effort, engine, image_model, audio_model, updated_at) " +
            $"VALUES ('{current}', '{current}', false, true, 'kimi/kimi-k2', NULL, 'local/remotion', 'local/z-image-turbo', NULL, now())");

        await profiles.GetService<IMigrator>().MigrateAsync(cancellationToken: ct);
        await production.GetService<IMigrator>().MigrateAsync(cancellationToken: ct);

        (await Row(database, "profiles.concept_settings", old, "concept_model, concept_effort, dedupe_model, dedupe_effort, topic_model, topic_effort, tone, word_check"))
            .ShouldBe("anthropic/claude-opus-5-5|high|anthropic/claude-haiku-5-5||openai/gpt-6.1-terra|low|samimi|true");
        (await Row(database, "profiles.concept_settings", current, "concept_model, concept_effort, dedupe_model, topic_model, topic_effort, word_check"))
            .ShouldBe("glm/glm-5.3|||anthropic/claude-sonnet-5-5|medium|false");
        (await Row(database, "production.settings", old, "code_model, code_effort, engine, image_model, audio_model, subtitles, progress_bar"))
            .ShouldBe("openai/gpt-6.1-sol|medium|local/hyperframes|openai/gpt-image-2.6-flare|local/supertonic-3|true|false");
        (await Row(database, "production.settings", current, "code_model, code_effort, engine, image_model, audio_model, subtitles, progress_bar"))
            .ShouldBe("kimi/kimi-k3.1-pro||local/remotion|local/z-image-turbo||false|true");
    }

    private static async Task<string> Row(PostgresTestDatabase database, string table, Guid profileId, string columns) =>
        (string)(await database.ScalarAsync(
            $"SELECT concat_ws('|', {string.Join(", ", columns.Split(", ").Select(c => $"coalesce({c}::text, '')"))}) FROM {table} WHERE profile_id = '{profileId}'"))!;
}
