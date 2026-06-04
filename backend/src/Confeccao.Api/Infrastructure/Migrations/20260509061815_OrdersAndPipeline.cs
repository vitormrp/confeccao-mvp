using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confeccao.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrdersAndPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "order_number_seq");

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('order_number_seq')"),
                    fabric_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_colors_color_id",
                        column: x => x.color_id,
                        principalTable: "colors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    size = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    planned_quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_items_models_model_id",
                        column: x => x.model_id,
                        principalTable: "models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    size = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    color_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fabric_code_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity_total = table.Column<int>(type: "integer", nullable: false),
                    quantity_done = table.Column<int>(type: "integer", nullable: false),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    laundry_package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pipeline_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_pipeline_items_models_model_id",
                        column: x => x.model_id,
                        principalTable: "models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pipeline_items_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pipeline_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pipeline_items_users_assigned_user_id",
                        column: x => x.assigned_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pipeline_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pipeline_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_pipeline_events_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_pipeline_events_pipeline_items_pipeline_item_id",
                        column: x => x.pipeline_item_id,
                        principalTable: "pipeline_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_pipeline_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_items_model_id",
                table: "order_items",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id_model_id_size",
                table: "order_items",
                columns: new[] { "order_id", "model_id", "size" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_color_id",
                table: "orders",
                column: "color_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_number",
                table: "orders",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_events_event_type",
                table: "pipeline_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_events_occurred_at",
                table: "pipeline_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_events_order_id",
                table: "pipeline_events",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_events_pipeline_item_id",
                table: "pipeline_events",
                column: "pipeline_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_events_user_id",
                table: "pipeline_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_items_assigned_user_id",
                table: "pipeline_items",
                column: "assigned_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_items_laundry_package_id",
                table: "pipeline_items",
                column: "laundry_package_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_items_model_id",
                table: "pipeline_items",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_items_order_id",
                table: "pipeline_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_items_order_item_id",
                table: "pipeline_items",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_items_stage_status",
                table: "pipeline_items",
                columns: new[] { "stage", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pipeline_events");

            migrationBuilder.DropTable(
                name: "pipeline_items");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropSequence(
                name: "order_number_seq");
        }
    }
}
