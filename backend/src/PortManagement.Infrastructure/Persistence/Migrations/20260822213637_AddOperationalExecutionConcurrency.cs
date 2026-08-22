using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalExecutionConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "port_management",
                table: "cargo_operations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                schema: "port_management",
                table: "cargo_operations");
        }
    }
}
