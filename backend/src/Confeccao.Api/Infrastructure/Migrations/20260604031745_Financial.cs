using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confeccao.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Financial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "payment_number_seq");

            migrationBuilder.CreateTable(
                name: "misc_costs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_misc_costs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('payment_number_seq')"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pipeline_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    size = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credits", x => x.id);
                    table.ForeignKey(
                        name: "fk_credits_models_model_id",
                        column: x => x.model_id,
                        principalTable: "models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_credits_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_credits_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_credits_pipeline_items_pipeline_item_id",
                        column: x => x.pipeline_item_id,
                        principalTable: "pipeline_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_credits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_credits_model_id",
                table: "credits",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_credits_occurred_at",
                table: "credits",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_credits_order_id",
                table: "credits",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_credits_payment_id",
                table: "credits",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_credits_pipeline_item_id",
                table: "credits",
                column: "pipeline_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_credits_user_id_payment_id",
                table: "credits",
                columns: new[] { "user_id", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_misc_costs_date",
                table: "misc_costs",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_payments_number",
                table: "payments",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_user_id",
                table: "payments",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credits");

            migrationBuilder.DropTable(
                name: "misc_costs");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropSequence(
                name: "payment_number_seq");
        }
    }
}
