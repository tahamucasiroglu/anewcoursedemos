using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkulYonetim.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Academics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_guardian_notices_guardian_user_id_student_id_day",
                table: "guardian_notices");

            migrationBuilder.AddColumn<Guid>(
                name: "source_id",
                table: "guardian_notices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text",
                table: "guardian_notices",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    classroom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_assessments_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assessments_classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "classrooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assessments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    classroom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignments_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignments_classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "classrooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "grades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grades", x => x.id);
                    table.CheckConstraint("ck_grades_score_range", "score IS NULL OR (score >= 0 AND score <= 100)");
                    table.ForeignKey(
                        name: "fk_grades_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grades_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grades_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grades_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assignment_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignment_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignment_submissions_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignment_submissions_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignment_submissions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignment_submissions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_guardian_notices_guardian_user_id_student_id_day",
                table: "guardian_notices",
                columns: new[] { "guardian_user_id", "student_id", "day" },
                unique: true,
                filter: "source_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_guardian_notices_guardian_user_id_student_id_source_id",
                table: "guardian_notices",
                columns: new[] { "guardian_user_id", "student_id", "source_id" },
                unique: true,
                filter: "source_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_assessments_branch_id",
                table: "assessments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_assessments_classroom_id_date",
                table: "assessments",
                columns: new[] { "classroom_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_assessments_created_by_user_id",
                table: "assessments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_assessments_tenant_id",
                table: "assessments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_submissions_assignment_id_student_id",
                table: "assignment_submissions",
                columns: new[] { "assignment_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assignment_submissions_student_id",
                table: "assignment_submissions",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_submissions_tenant_id",
                table: "assignment_submissions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_submissions_updated_by_user_id",
                table: "assignment_submissions",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignments_branch_id",
                table: "assignments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignments_classroom_id_due_date",
                table: "assignments",
                columns: new[] { "classroom_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_assignments_created_by_user_id",
                table: "assignments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignments_tenant_id",
                table: "assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_grades_assessment_id_student_id",
                table: "grades",
                columns: new[] { "assessment_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grades_student_id",
                table: "grades",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_grades_tenant_id",
                table: "grades",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_grades_updated_by_user_id",
                table: "grades",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_submissions");

            migrationBuilder.DropTable(
                name: "grades");

            migrationBuilder.DropTable(
                name: "assignments");

            migrationBuilder.DropTable(
                name: "assessments");

            migrationBuilder.DropIndex(
                name: "ix_guardian_notices_guardian_user_id_student_id_day",
                table: "guardian_notices");

            migrationBuilder.DropIndex(
                name: "ix_guardian_notices_guardian_user_id_student_id_source_id",
                table: "guardian_notices");

            migrationBuilder.DropColumn(
                name: "source_id",
                table: "guardian_notices");

            migrationBuilder.DropColumn(
                name: "text",
                table: "guardian_notices");

            migrationBuilder.CreateIndex(
                name: "ix_guardian_notices_guardian_user_id_student_id_day",
                table: "guardian_notices",
                columns: new[] { "guardian_user_id", "student_id", "day" },
                unique: true);
        }
    }
}
