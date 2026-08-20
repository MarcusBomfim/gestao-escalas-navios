using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "port_management");

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ports",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    un_locode = table.Column<string>(type: "character(5)", fixedLength: true, maxLength: 5, nullable: false),
                    country_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vessels",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    imo_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    flag_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    length_overall_meters = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    beam_meters = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    maximum_draft_meters = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    call_sign = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    mmsi = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vessels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "terminals",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    port_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terminals", x => x.id);
                    table.ForeignKey(
                        name: "fk_terminals_ports_port_id",
                        column: x => x.port_id,
                        principalSchema: "port_management",
                        principalTable: "ports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "berths",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    terminal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    useful_length_meters = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    maximum_beam_meters = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    maximum_draft_meters = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    supported_vessel_types = table.Column<int[]>(type: "integer[]", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_berths", x => x.id);
                    table.ForeignKey(
                        name: "fk_berths_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalSchema: "port_management",
                        principalTable: "terminals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "port_calls",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_code = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    port_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shipping_line_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planned_terminal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planned_berth_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    voyage_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    previous_port_un_locode = table.Column<string>(type: "character(5)", fixedLength: true, maxLength: 5, nullable: true),
                    next_port_un_locode = table.Column<string>(type: "character(5)", fixedLength: true, maxLength: 5, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_port_calls", x => x.id);
                    table.ForeignKey(
                        name: "fk_port_calls_berths_planned_berth_id",
                        column: x => x.planned_berth_id,
                        principalSchema: "port_management",
                        principalTable: "berths",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_port_calls_organizations_agent_organization_id",
                        column: x => x.agent_organization_id,
                        principalSchema: "port_management",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_port_calls_organizations_shipping_line_organization_id",
                        column: x => x.shipping_line_organization_id,
                        principalSchema: "port_management",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_port_calls_ports_port_id",
                        column: x => x.port_id,
                        principalSchema: "port_management",
                        principalTable: "ports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_port_calls_terminals_planned_terminal_id",
                        column: x => x.planned_terminal_id,
                        principalSchema: "port_management",
                        principalTable: "terminals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_port_calls_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "port_management",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "berth_windows",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    port_call_id = table.Column<Guid>(type: "uuid", nullable: false),
                    berth_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    requested_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    last_change_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_berth_windows", x => x.id);
                    table.CheckConstraint("ck_berth_windows_valid_period", "ends_at_utc > starts_at_utc");
                    table.ForeignKey(
                        name: "fk_berth_windows_berths_berth_id",
                        column: x => x.berth_id,
                        principalSchema: "port_management",
                        principalTable: "berths",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_berth_windows_port_calls_port_call_id",
                        column: x => x.port_call_id,
                        principalSchema: "port_management",
                        principalTable: "port_calls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cargo_operations",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    port_call_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cargo_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    planned_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    actual_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    quantity_unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_dangerous_cargo = table.Column<bool>(type: "boolean", nullable: false),
                    dangerous_cargo_classification = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    planned_start_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    planned_end_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_start_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_end_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cargo_operations", x => x.id);
                    table.CheckConstraint("ck_cargo_operations_actual_quantity", "actual_quantity IS NULL OR actual_quantity >= 0");
                    table.CheckConstraint("ck_cargo_operations_dangerous_classification", "NOT is_dangerous_cargo OR dangerous_cargo_classification IS NOT NULL");
                    table.CheckConstraint("ck_cargo_operations_planned_quantity", "planned_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_cargo_operations_port_calls_port_call_id",
                        column: x => x.port_call_id,
                        principalSchema: "port_management",
                        principalTable: "port_calls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "port_call_events",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    port_call_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    classifier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    occurs_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recorded_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    replaces_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correction_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_port_call_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_port_call_events_port_call_events_replaces_event_id",
                        column: x => x.replaces_event_id,
                        principalSchema: "port_management",
                        principalTable: "port_call_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_port_call_events_port_calls_port_call_id",
                        column: x => x.port_call_id,
                        principalSchema: "port_management",
                        principalTable: "port_calls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "port_call_status_history",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    port_call_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    new_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    changed_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_port_call_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_port_call_status_history_port_calls_port_call_id",
                        column: x => x.port_call_id,
                        principalSchema: "port_management",
                        principalTable: "port_calls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "berth_window_revisions",
                schema: "port_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    berth_window_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    previous_ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    new_starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    new_ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_berth_window_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_berth_window_revisions_berth_windows_berth_window_id",
                        column: x => x.berth_window_id,
                        principalSchema: "port_management",
                        principalTable: "berth_windows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_berth_window_revisions_berth_window_id_changed_at_utc",
                schema: "port_management",
                table: "berth_window_revisions",
                columns: new[] { "berth_window_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_berth_windows_berth_id_starts_at_utc_ends_at_utc",
                schema: "port_management",
                table: "berth_windows",
                columns: new[] { "berth_id", "starts_at_utc", "ends_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_berth_windows_port_call_id_status",
                schema: "port_management",
                table: "berth_windows",
                columns: new[] { "port_call_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_berths_terminal_id_code",
                schema: "port_management",
                table: "berths",
                columns: new[] { "terminal_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cargo_operations_port_call_id",
                schema: "port_management",
                table: "cargo_operations",
                column: "port_call_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_registration_number",
                schema: "port_management",
                table: "organizations",
                column: "registration_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizations_type_is_active",
                schema: "port_management",
                table: "organizations",
                columns: new[] { "type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_port_call_events_port_call_id_phase_action_classifier_occur",
                schema: "port_management",
                table: "port_call_events",
                columns: new[] { "port_call_id", "phase", "action", "classifier", "occurs_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_port_call_events_replaces_event_id",
                schema: "port_management",
                table: "port_call_events",
                column: "replaces_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_port_call_status_history_port_call_id_changed_at_utc",
                schema: "port_management",
                table: "port_call_status_history",
                columns: new[] { "port_call_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_port_calls_agent_organization_id",
                schema: "port_management",
                table: "port_calls",
                column: "agent_organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_port_calls_idempotency_key",
                schema: "port_management",
                table: "port_calls",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_port_calls_planned_berth_id_status",
                schema: "port_management",
                table: "port_calls",
                columns: new[] { "planned_berth_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_port_calls_planned_terminal_id",
                schema: "port_management",
                table: "port_calls",
                column: "planned_terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_port_calls_port_id_status",
                schema: "port_management",
                table: "port_calls",
                columns: new[] { "port_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_port_calls_public_code",
                schema: "port_management",
                table: "port_calls",
                column: "public_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_port_calls_shipping_line_organization_id",
                schema: "port_management",
                table: "port_calls",
                column: "shipping_line_organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_port_calls_vessel_id",
                schema: "port_management",
                table: "port_calls",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_ports_un_locode",
                schema: "port_management",
                table: "ports",
                column: "un_locode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_terminals_port_id_code",
                schema: "port_management",
                table: "terminals",
                columns: new[] { "port_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vessels_imo_number",
                schema: "port_management",
                table: "vessels",
                column: "imo_number",
                unique: true,
                filter: "imo_number IS NOT NULL AND is_active");

            migrationBuilder.CreateIndex(
                name: "ix_vessels_name",
                schema: "port_management",
                table: "vessels",
                column: "name");

            migrationBuilder.Sql(
                """
                ALTER TABLE port_management.berth_windows
                ADD CONSTRAINT ex_berth_windows_no_confirmed_overlap
                EXCLUDE USING gist
                (
                    berth_id WITH =,
                    tstzrange(starts_at_utc, ends_at_utc, '[)') WITH &&
                )
                WHERE (status = 'Confirmed');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "berth_window_revisions",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "cargo_operations",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "port_call_events",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "port_call_status_history",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "berth_windows",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "port_calls",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "berths",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "vessels",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "terminals",
                schema: "port_management");

            migrationBuilder.DropTable(
                name: "ports",
                schema: "port_management");
        }
    }
}
