using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkulYonetim.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Communication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "announcements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    consent_question = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    consent_deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcements", x => x.id);
                    table.ForeignKey(
                        name: "fk_announcements_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_announcements_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_message_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    teacher_read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    guardian_read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversations_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conversations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conversations_users_guardian_user_id",
                        column: x => x.guardian_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conversations_users_teacher_user_id",
                        column: x => x.teacher_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quiet_hours",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiet_hours", x => x.id);
                    table.ForeignKey(
                        name: "fk_quiet_hours_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quiet_hours_users_teacher_user_id",
                        column: x => x.teacher_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "announcement_recipients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcement_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_announcement_recipients_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_announcement_recipients_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_announcement_recipients_users_guardian_user_id",
                        column: x => x.guardian_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "announcement_students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcement_students", x => x.id);
                    table.ForeignKey(
                        name: "fk_announcement_students_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_announcement_students_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_announcement_students_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consent_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    answered_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_responses", x => x.id);
                    table.ForeignKey(
                        name: "fk_consent_responses_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_responses_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_responses_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_responses_users_answered_by_user_id",
                        column: x => x.answered_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversation_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversation_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conversation_messages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conversation_messages_users_sender_user_id",
                        column: x => x.sender_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_announcement_recipients_announcement_id_guardian_user_id",
                table: "announcement_recipients",
                columns: new[] { "announcement_id", "guardian_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_announcement_recipients_guardian_user_id",
                table: "announcement_recipients",
                column: "guardian_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_announcement_recipients_tenant_id_guardian_user_id",
                table: "announcement_recipients",
                columns: new[] { "tenant_id", "guardian_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_announcement_students_announcement_id_student_id",
                table: "announcement_students",
                columns: new[] { "announcement_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_announcement_students_student_id",
                table: "announcement_students",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_announcement_students_tenant_id",
                table: "announcement_students",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_announcements_author_user_id",
                table: "announcements",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_announcements_tenant_id_author_user_id_published_at",
                table: "announcements",
                columns: new[] { "tenant_id", "author_user_id", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ix_announcements_tenant_id_published_at",
                table: "announcements",
                columns: new[] { "tenant_id", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ix_consent_responses_announcement_id_student_id",
                table: "consent_responses",
                columns: new[] { "announcement_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_consent_responses_answered_by_user_id",
                table: "consent_responses",
                column: "answered_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_responses_student_id",
                table: "consent_responses",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_responses_tenant_id",
                table: "consent_responses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversation_messages_conversation_id_sent_at",
                table: "conversation_messages",
                columns: new[] { "conversation_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_conversation_messages_sender_user_id",
                table: "conversation_messages",
                column: "sender_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversation_messages_tenant_id",
                table: "conversation_messages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_guardian_user_id",
                table: "conversations",
                column: "guardian_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_student_id",
                table: "conversations",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_teacher_user_id",
                table: "conversations",
                column: "teacher_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_tenant_id_guardian_user_id_last_message_at",
                table: "conversations",
                columns: new[] { "tenant_id", "guardian_user_id", "last_message_at" });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_tenant_id_teacher_user_id_guardian_user_id_st",
                table: "conversations",
                columns: new[] { "tenant_id", "teacher_user_id", "guardian_user_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_tenant_id_teacher_user_id_last_message_at",
                table: "conversations",
                columns: new[] { "tenant_id", "teacher_user_id", "last_message_at" });

            migrationBuilder.CreateIndex(
                name: "ix_quiet_hours_teacher_user_id",
                table: "quiet_hours",
                column: "teacher_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_quiet_hours_tenant_id_teacher_user_id",
                table: "quiet_hours",
                columns: new[] { "tenant_id", "teacher_user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcement_recipients");

            migrationBuilder.DropTable(
                name: "announcement_students");

            migrationBuilder.DropTable(
                name: "consent_responses");

            migrationBuilder.DropTable(
                name: "conversation_messages");

            migrationBuilder.DropTable(
                name: "quiet_hours");

            migrationBuilder.DropTable(
                name: "announcements");

            migrationBuilder.DropTable(
                name: "conversations");
        }
    }
}
