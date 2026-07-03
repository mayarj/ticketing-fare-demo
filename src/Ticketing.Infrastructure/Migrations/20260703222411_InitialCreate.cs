using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "current_fare_rates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    policy_code = table.Column<string>(type: "varchar(50)", nullable: false),
                    @params = table.Column<string>(name: "params", type: "nvarchar(max)", nullable: false),
                    effective_from = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_current_fare_rates", x => x.id);
                    table.CheckConstraint("CHK_current_fare_rates_json", "ISJSON([params]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "current_modification_rules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    modification_code = table.Column<string>(type: "varchar(50)", nullable: false),
                    rule_type = table.Column<string>(type: "varchar(50)", nullable: false),
                    @params = table.Column<string>(name: "params", type: "nvarchar(max)", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_current_modification_rules", x => x.id);
                    table.CheckConstraint("CHK_current_mod_rules_json", "ISJSON([params]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ticket_number = table.Column<string>(type: "varchar(50)", nullable: false),
                    product_type = table.Column<string>(type: "varchar(50)", nullable: false),
                    issued_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    base_fare = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    total_fare = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "applied_ticket_modifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ticket_id = table.Column<long>(type: "bigint", nullable: false),
                    modification_code = table.Column<string>(type: "varchar(50)", nullable: false),
                    rule_type = table.Column<string>(type: "varchar(50)", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    params_used = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    surcharge = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    applied_order = table.Column<int>(type: "int", nullable: false),
                    applied_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_applied_ticket_modifications", x => x.id);
                    table.CheckConstraint("CHK_applied_params_json", "ISJSON([params_used]) = 1");
                    table.ForeignKey(
                        name: "FK_applied_ticket_modifications_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fare_calculation_snapshots",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ticket_id = table.Column<long>(type: "bigint", nullable: false),
                    policy_code = table.Column<string>(type: "varchar(50)", nullable: false),
                    base_fare_inputs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    calculated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fare_calculation_snapshots", x => x.id);
                    table.CheckConstraint("CHK_snapshot_inputs_json", "ISJSON([base_fare_inputs]) = 1");
                    table.ForeignKey(
                        name: "FK_fare_calculation_snapshots_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_applied_ticket_modifications_ticket_id",
                table: "applied_ticket_modifications",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "UQ_current_fare_rates_policy",
                table: "current_fare_rates",
                column: "policy_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_current_mod_rules_code",
                table: "current_modification_rules",
                column: "modification_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fare_calculation_snapshots_ticket_id",
                table: "fare_calculation_snapshots",
                column: "ticket_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_tickets_ticket_number",
                table: "tickets",
                column: "ticket_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "applied_ticket_modifications");

            migrationBuilder.DropTable(
                name: "current_fare_rates");

            migrationBuilder.DropTable(
                name: "current_modification_rules");

            migrationBuilder.DropTable(
                name: "fare_calculation_snapshots");

            migrationBuilder.DropTable(
                name: "tickets");
        }
    }
}
