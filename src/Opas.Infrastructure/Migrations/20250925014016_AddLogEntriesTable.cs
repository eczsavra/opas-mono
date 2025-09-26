using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLogEntriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "log_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    client_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    request_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    request_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    exception = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    properties = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_correlation_id",
                table: "log_entries",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_level",
                table: "log_entries",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_tenant_id",
                table: "log_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_timestamp",
                table: "log_entries",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_user_id",
                table: "log_entries",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_entries");
        }
    }
}
