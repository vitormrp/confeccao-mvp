using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confeccao.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LaundryPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "laundry_package_number_seq");

            migrationBuilder.CreateTable(
                name: "laundry_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('laundry_package_number_seq')"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_laundry_packages", x => x.id);
                    table.ForeignKey(
                        name: "fk_laundry_packages_users_completed_by_user_id",
                        column: x => x.completed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_laundry_packages_completed_by_user_id",
                table: "laundry_packages",
                column: "completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_laundry_packages_number",
                table: "laundry_packages",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_laundry_packages_status",
                table: "laundry_packages",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "fk_pipeline_items_laundry_packages_laundry_package_id",
                table: "pipeline_items",
                column: "laundry_package_id",
                principalTable: "laundry_packages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pipeline_items_laundry_packages_laundry_package_id",
                table: "pipeline_items");

            migrationBuilder.DropTable(
                name: "laundry_packages");

            migrationBuilder.DropSequence(
                name: "laundry_package_number_seq");
        }
    }
}
