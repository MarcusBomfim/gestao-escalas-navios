using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBerthWindowConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "port_management",
                table: "berth_windows",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "new_berth_id",
                schema: "port_management",
                table: "berth_window_revisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "previous_berth_id",
                schema: "port_management",
                table: "berth_window_revisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE port_management.berth_window_revisions AS revision
                SET previous_berth_id = berth_window.berth_id,
                    new_berth_id = berth_window.berth_id
                FROM port_management.berth_windows AS berth_window
                WHERE revision.berth_window_id = berth_window.id;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "new_berth_id",
                schema: "port_management",
                table: "berth_window_revisions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "previous_berth_id",
                schema: "port_management",
                table: "berth_window_revisions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_berth_windows_port_call_id",
                schema: "port_management",
                table: "berth_windows",
                column: "port_call_id",
                unique: true,
                filter: "status IN ('Requested', 'Confirmed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_berth_windows_port_call_id",
                schema: "port_management",
                table: "berth_windows");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "port_management",
                table: "berth_windows");

            migrationBuilder.DropColumn(
                name: "new_berth_id",
                schema: "port_management",
                table: "berth_window_revisions");

            migrationBuilder.DropColumn(
                name: "previous_berth_id",
                schema: "port_management",
                table: "berth_window_revisions");
        }
    }
}
