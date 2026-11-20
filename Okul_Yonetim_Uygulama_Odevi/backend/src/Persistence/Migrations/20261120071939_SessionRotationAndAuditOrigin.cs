using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkulYonetim.Persistence.Migrations
{
    /// <summary>
    /// Spec 0004 review düzeltmesi: oturumda bir önceki yenileme anahtarının özeti ve değişme zamanı (eşzamanlı yenileme
    /// toleransı ve çalınan anahtar tespiti); audit kaydında işlemin kaynağı (komut satırı işlemleri için "kim").
    /// </summary>
    public partial class SessionRotationAndAuditOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "previous_refresh_token_hash",
                table: "user_sessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "rotated_at",
                table: "user_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin",
                table: "audit_entries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_previous_refresh_token_hash",
                table: "user_sessions",
                column: "previous_refresh_token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_sessions_previous_refresh_token_hash",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "previous_refresh_token_hash",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "rotated_at",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "audit_entries");
        }
    }
}
