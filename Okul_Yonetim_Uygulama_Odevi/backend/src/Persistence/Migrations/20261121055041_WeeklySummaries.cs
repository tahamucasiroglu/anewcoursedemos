using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkulYonetim.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WeeklySummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "text",
                table: "guardian_notices",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400,
                oldNullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "period_start",
                table: "ai_drafts",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_drafts_tenant_id_kind_student_id_period_start",
                table: "ai_drafts",
                columns: new[] { "tenant_id", "kind", "student_id", "period_start" },
                unique: true,
                filter: "period_start IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ai_drafts_tenant_id_kind_student_id_period_start",
                table: "ai_drafts");

            migrationBuilder.DropColumn(
                name: "period_start",
                table: "ai_drafts");

            migrationBuilder.AlterColumn<string>(
                name: "text",
                table: "guardian_notices",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
