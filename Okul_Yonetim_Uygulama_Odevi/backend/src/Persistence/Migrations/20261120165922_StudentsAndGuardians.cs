using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkulYonetim.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudentsAndGuardians : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    classroom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    school_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    school_number_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    health_note_cipher = table.Column<string>(type: "text", nullable: true),
                    allergy_cipher = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_students", x => x.id);
                    table.ForeignKey(
                        name: "fk_students_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_students_classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "classrooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_students_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_students_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_students_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pickup_persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    relationship = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pickup_persons", x => x.id);
                    table.ForeignKey(
                        name: "fk_pickup_persons_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pickup_persons_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_guardians",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_guardians", x => x.id);
                    table.ForeignKey(
                        name: "fk_student_guardians_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_guardians_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_guardians_users_guardian_user_id",
                        column: x => x.guardian_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pickup_persons_student_id",
                table: "pickup_persons",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_pickup_persons_tenant_id",
                table: "pickup_persons",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_guardians_guardian_user_id",
                table: "student_guardians",
                column: "guardian_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_guardians_student_id_guardian_user_id",
                table: "student_guardians",
                columns: new[] { "student_id", "guardian_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_guardians_tenant_id_guardian_user_id",
                table: "student_guardians",
                columns: new[] { "tenant_id", "guardian_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_students_branch_id",
                table: "students",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_students_classroom_id",
                table: "students",
                column: "classroom_id");

            migrationBuilder.CreateIndex(
                name: "ix_students_created_by",
                table: "students",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_students_tenant_id",
                table: "students",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_students_tenant_id_school_number_key",
                table: "students",
                columns: new[] { "tenant_id", "school_number_key" },
                unique: true,
                filter: "school_number_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_students_updated_by",
                table: "students",
                column: "updated_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pickup_persons");

            migrationBuilder.DropTable(
                name: "student_guardians");

            migrationBuilder.DropTable(
                name: "students");
        }
    }
}
