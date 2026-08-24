using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_records",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    changed_fields = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    request_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_entity_type_occurred_at_utc",
                schema: "port_management",
                table: "audit_records",
                columns: new[] { "entity_type", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_occurred_at_utc",
                schema: "port_management",
                table: "audit_records",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_user_id_occurred_at_utc",
                schema: "port_management",
                table: "audit_records",
                columns: new[] { "user_id", "occurred_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_records",
                schema: "port_management");
        }
    }
}
