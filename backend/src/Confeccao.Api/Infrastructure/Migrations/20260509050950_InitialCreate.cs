using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confeccao.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "colors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hex_code = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    has_lining = table.Column<bool>(type: "boolean", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    button_count = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_models", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "model_stages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_model_stages", x => x.id);
                    table.ForeignKey(
                        name: "fk_model_stages_models_model_id",
                        column: x => x.model_id,
                        principalTable: "models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    lining_extra = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    interfacing_extra = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    covered_button_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    ready_button_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_prices_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_tiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    up_to_quantity = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_tiers", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_tiers_prices_price_id",
                        column: x => x.price_id,
                        principalTable: "prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_colors_name",
                table: "colors",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_model_stages_model_id_sequence",
                table: "model_stages",
                columns: new[] { "model_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_model_stages_model_id_stage",
                table: "model_stages",
                columns: new[] { "model_id", "stage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_models_name",
                table: "models",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_price_tiers_price_id",
                table: "price_tiers",
                column: "price_id");

            migrationBuilder.CreateIndex(
                name: "ix_prices_user_id_effective_from",
                table: "prices",
                columns: new[] { "user_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "colors");

            migrationBuilder.DropTable(
                name: "model_stages");

            migrationBuilder.DropTable(
                name: "price_tiers");

            migrationBuilder.DropTable(
                name: "models");

            migrationBuilder.DropTable(
                name: "prices");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
