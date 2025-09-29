using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opas.Infrastructure.Migrations.PublicDb
{
    /// <inheritdoc />
    public partial class AddCentralProductsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "central_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gtin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    drug_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    manufacturer_gln = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    manufacturer_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    imported = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_central_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gln_registry",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gln = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    authorized = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    town = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: true),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    imported_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gln_registry", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_central_products_active",
                table: "central_products",
                column: "active");

            migrationBuilder.CreateIndex(
                name: "IX_central_products_drug_name",
                table: "central_products",
                column: "drug_name");

            migrationBuilder.CreateIndex(
                name: "IX_central_products_gtin",
                table: "central_products",
                column: "gtin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_central_products_manufacturer_gln",
                table: "central_products",
                column: "manufacturer_gln");

            migrationBuilder.CreateIndex(
                name: "IX_gln_registry_gln",
                table: "gln_registry",
                column: "gln",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "central_products");

            migrationBuilder.DropTable(
                name: "gln_registry");
        }
    }
}
