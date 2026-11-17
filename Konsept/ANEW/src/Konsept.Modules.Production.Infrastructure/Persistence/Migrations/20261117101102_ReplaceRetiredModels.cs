using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konsept.Modules.Production.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Kayıtlı üretim ayarlarındaki (kod ve görsel modeli) katalogdan kaldırılan model kimliklerini yerine geçen kimliğe taşır (spec 0026 Req 3);
    /// efor değerleri yeni modelde geçerlidir, değişmez. Sabitlenmiş iş ayarları geçmiş kayıttır; çağrıları AiGateway eşlemesiyle çözülür.
    /// Yalnızca veri taşımasıdır; geri alınmaz (Down boş).
    /// </summary>
    public partial class ReplaceRetiredModels : Migration
    {
        /// <summary>Katalogdaki <c>replacedModels</c> ile aynı eşleme (spec 0026; katalog sonradan değişse de bu taşıma sabittir).</summary>
        public static IReadOnlyList<(string Old, string New)> Replaced { get; } =
        [
            ("anthropic/claude-opus-4-5", "anthropic/claude-opus-5-5"),
            ("anthropic/claude-sonnet-4-5", "anthropic/claude-sonnet-5-5"),
            ("anthropic/claude-haiku-4-5", "anthropic/claude-haiku-5-5"),
            ("openai/gpt-5", "openai/gpt-6.1-sol"),
            ("openai/gpt-5-mini", "openai/gpt-6.1-terra"),
            ("openai/gpt-image-1", "openai/gpt-image-2.6-flare"),
            ("openrouter/anthropic/claude-sonnet-4.5", "openrouter/anthropic/claude-sonnet-5.5"),
            ("glm/glm-4.6", "glm/glm-5.3"),
            ("kimi/kimi-k2", "kimi/kimi-k3.1-pro"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            var values = string.Join(", ", Replaced.Select(r => $"('{r.Old}', '{r.New}')"));
            foreach (var column in new[] { "code_model", "image_model" })
            {
                migrationBuilder.Sql(
                    $"UPDATE production.settings AS s SET {column} = m.new_id FROM (VALUES {values}) AS m(old_id, new_id) WHERE s.{column} = m.old_id;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
