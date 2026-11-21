using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkulYonetim.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Billing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discount_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    percent = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_definitions", x => x.id);
                    table.CheckConstraint("ck_discount_definitions_percent", "percent >= 1 AND percent <= 100");
                    table.ForeignKey(
                        name: "fk_discount_definitions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_schedules", x => x.id);
                    table.CheckConstraint("ck_fee_schedules_amount", "amount > 0 AND amount <= 10000000.00");
                    table.ForeignKey(
                        name: "fk_fee_schedules_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receipt_counters",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_number = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_counters", x => x.tenant_id);
                    table.CheckConstraint("ck_receipt_counters_last", "last_number >= 0");
                    table.ForeignKey(
                        name: "fk_receipt_counters_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "installment_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    discount_percent = table.Column<int>(type: "integer", nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    installment_count = table.Column<int>(type: "integer", nullable: false),
                    first_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installment_plans", x => x.id);
                    table.CheckConstraint("ck_installment_plans_count", "installment_count >= 1 AND installment_count <= 12");
                    table.CheckConstraint("ck_installment_plans_net", "net_amount > 0 AND discount_amount >= 0 AND net_amount = fee_amount - discount_amount");
                    table.ForeignKey(
                        name: "fk_installment_plans_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installment_plans_discount_definitions_discount_id",
                        column: x => x.discount_id,
                        principalTable: "discount_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installment_plans_fee_schedules_fee_schedule_id",
                        column: x => x.fee_schedule_id,
                        principalTable: "fee_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installment_plans_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installment_plans_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "installments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installments", x => x.id);
                    table.CheckConstraint("ck_installments_amounts", "amount > 0 AND paid_amount >= 0 AND paid_amount <= amount");
                    table.ForeignKey(
                        name: "fk_installments_installment_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "installment_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    receipt_no = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount", "amount > 0 AND receipt_no >= 1");
                    table.ForeignKey(
                        name: "fk_payments_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_installment_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "installment_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_reminders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_reminders", x => x.id);
                    table.CheckConstraint("ck_payment_reminders_sequence", "sequence >= 1 AND sequence <= 3");
                    table.ForeignKey(
                        name: "fk_payment_reminders_installments_installment_id",
                        column: x => x.installment_id,
                        principalTable: "installments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_reminders_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocations", x => x.id);
                    table.CheckConstraint("ck_payment_allocations_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_allocations_installments_installment_id",
                        column: x => x.installment_id,
                        principalTable: "installments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discount_definitions_tenant_id",
                table: "discount_definitions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_schedules_tenant_id",
                table: "fee_schedules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_installment_plans_branch_id",
                table: "installment_plans",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_installment_plans_discount_id",
                table: "installment_plans",
                column: "discount_id");

            migrationBuilder.CreateIndex(
                name: "ix_installment_plans_fee_schedule_id",
                table: "installment_plans",
                column: "fee_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_installment_plans_student_id_fee_schedule_id",
                table: "installment_plans",
                columns: new[] { "student_id", "fee_schedule_id" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_installment_plans_tenant_id_branch_id",
                table: "installment_plans",
                columns: new[] { "tenant_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_installments_plan_id_sequence",
                table: "installments",
                columns: new[] { "plan_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_installments_tenant_id_due_date",
                table: "installments",
                columns: new[] { "tenant_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_installment_id",
                table: "payment_allocations",
                column: "installment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_payment_id_installment_id",
                table: "payment_allocations",
                columns: new[] { "payment_id", "installment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_tenant_id",
                table: "payment_allocations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_reminders_installment_id_sequence",
                table: "payment_reminders",
                columns: new[] { "installment_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_reminders_tenant_id",
                table: "payment_reminders",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_branch_id",
                table: "payments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_plan_id",
                table: "payments",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_recorded_by",
                table: "payments",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "ix_payments_student_id",
                table: "payments",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_id_receipt_no",
                table: "payments",
                columns: new[] { "tenant_id", "receipt_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "payment_reminders");

            migrationBuilder.DropTable(
                name: "receipt_counters");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "installments");

            migrationBuilder.DropTable(
                name: "installment_plans");

            migrationBuilder.DropTable(
                name: "discount_definitions");

            migrationBuilder.DropTable(
                name: "fee_schedules");
        }
    }
}
