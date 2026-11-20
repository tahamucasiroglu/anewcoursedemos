using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkulYonetim.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceAndNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "detail",
                table: "audit_entries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "attendance_sheets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    classroom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_sheets", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendance_sheets_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_sheets_classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "classrooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_sheets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_sheets_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "classroom_teachers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    classroom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_classroom_teachers", x => x.id);
                    table.ForeignKey(
                        name: "fk_classroom_teachers_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_classroom_teachers_classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "classrooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_classroom_teachers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_classroom_teachers_users_teacher_user_id",
                        column: x => x.teacher_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guardian_notices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guardian_notices", x => x.id);
                    table.ForeignKey(
                        name: "fk_guardian_notices_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_guardian_notices_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_guardian_notices_users_guardian_user_id",
                        column: x => x.guardian_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sheet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendance_entries_attendance_sheets_sheet_id",
                        column: x => x.sheet_id,
                        principalTable: "attendance_sheets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_entries_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_entries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_entries_sheet_id_student_id",
                table: "attendance_entries",
                columns: new[] { "sheet_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_entries_student_id",
                table: "attendance_entries",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_entries_tenant_id",
                table: "attendance_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_sheets_branch_id",
                table: "attendance_sheets",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_sheets_classroom_id_day",
                table: "attendance_sheets",
                columns: new[] { "classroom_id", "day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_sheets_recorded_by",
                table: "attendance_sheets",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_sheets_tenant_id_day",
                table: "attendance_sheets",
                columns: new[] { "tenant_id", "day" });

            migrationBuilder.CreateIndex(
                name: "ix_classroom_teachers_branch_id",
                table: "classroom_teachers",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_classroom_teachers_classroom_id_teacher_user_id",
                table: "classroom_teachers",
                columns: new[] { "classroom_id", "teacher_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_classroom_teachers_teacher_user_id",
                table: "classroom_teachers",
                column: "teacher_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_classroom_teachers_tenant_id_teacher_user_id",
                table: "classroom_teachers",
                columns: new[] { "tenant_id", "teacher_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guardian_notices_guardian_user_id_student_id_day",
                table: "guardian_notices",
                columns: new[] { "guardian_user_id", "student_id", "day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guardian_notices_student_id",
                table: "guardian_notices",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_guardian_notices_tenant_id_guardian_user_id_updated_at",
                table: "guardian_notices",
                columns: new[] { "tenant_id", "guardian_user_id", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_entries");

            migrationBuilder.DropTable(
                name: "classroom_teachers");

            migrationBuilder.DropTable(
                name: "guardian_notices");

            migrationBuilder.DropTable(
                name: "attendance_sheets");

            migrationBuilder.DropColumn(
                name: "detail",
                table: "audit_entries");
        }
    }
}
