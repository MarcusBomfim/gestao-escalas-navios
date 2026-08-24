using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_receipts",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_receipts_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_receipts_user_id_alert_id",
                schema: "port_management",
                table: "notification_receipts",
                columns: new[] { "user_id", "alert_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_receipts",
                schema: "port_management");
        }
    }
}
