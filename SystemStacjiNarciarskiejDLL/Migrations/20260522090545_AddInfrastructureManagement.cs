using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SystemStacjiNarciarskiejDLL.Migrations
{
    /// <inheritdoc />
    public partial class AddInfrastructureManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "administrator",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administrator", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cashier",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashier", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_card_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_card_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_lift_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_lift_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_operation_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_operation_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_pass_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_pass_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_pass_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_pass_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_report_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_report_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_reservation_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_reservation_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_season",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_season", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_trail_difficulty",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_trail_difficulty", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_trail_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_trail_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dict_verification_result",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dict_verification_result", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trail_planner",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trail_planner", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shift_report",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cashier_id = table.Column<int>(type: "integer", nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_revenue = table.Column<decimal>(type: "numeric", nullable: true),
                    total_deposit_returns = table.Column<decimal>(type: "numeric", nullable: true),
                    cards_issued_count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_report", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_report_cashier_cashier_id",
                        column: x => x.cashier_id,
                        principalTable: "cashier",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "admin_report",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admin_id = table.Column<int>(type: "integer", nullable: true),
                    report_type_id = table.Column<int>(type: "integer", nullable: true),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    report_parameters = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_report", x => x.id);
                    table.ForeignKey(
                        name: "FK_admin_report_administrator_admin_id",
                        column: x => x.admin_id,
                        principalTable: "administrator",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_admin_report_dict_report_type_report_type_id",
                        column: x => x.report_type_id,
                        principalTable: "dict_report_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tariff",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: true),
                    pass_type_id = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric", nullable: true),
                    ride_count = table.Column<int>(type: "integer", nullable: true),
                    pool_limit = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tariff", x => x.id);
                    table.ForeignKey(
                        name: "FK_tariff_dict_pass_type_pass_type_id",
                        column: x => x.pass_type_id,
                        principalTable: "dict_pass_type",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tariff_dict_season_season_id",
                        column: x => x.season_id,
                        principalTable: "dict_season",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lift",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    location = table.Column<string>(type: "text", nullable: true),
                    length = table.Column<decimal>(type: "numeric", nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<string>(type: "text", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: true),
                    planner_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lift", x => x.id);
                    table.ForeignKey(
                        name: "FK_lift_dict_lift_status_status_id",
                        column: x => x.status_id,
                        principalTable: "dict_lift_status",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_lift_trail_planner_planner_id",
                        column: x => x.planner_id,
                        principalTable: "trail_planner",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "trail",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    location = table.Column<string>(type: "text", nullable: true),
                    length = table.Column<decimal>(type: "numeric", nullable: true),
                    difficulty_id = table.Column<int>(type: "integer", nullable: true),
                    snow_condition = table.Column<string>(type: "text", nullable: true),
                    preparation_status = table.Column<string>(type: "text", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: true),
                    planner_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trail", x => x.id);
                    table.ForeignKey(
                        name: "FK_trail_dict_trail_difficulty_difficulty_id",
                        column: x => x.difficulty_id,
                        principalTable: "dict_trail_difficulty",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trail_dict_trail_status_status_id",
                        column: x => x.status_id,
                        principalTable: "dict_trail_status",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trail_trail_planner_planner_id",
                        column: x => x.planner_id,
                        principalTable: "trail_planner",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "card",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    deposit_paid = table.Column<bool>(type: "boolean", nullable: true),
                    block_reason = table.Column<string>(type: "text", nullable: true),
                    physical_condition = table.Column<string>(type: "text", nullable: true),
                    added_to_pool_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card", x => x.id);
                    table.ForeignKey(
                        name: "FK_card_dict_card_status_status_id",
                        column: x => x.status_id,
                        principalTable: "dict_card_status",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_card_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reservation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reservation_number = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    reservation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation", x => x.id);
                    table.ForeignKey(
                        name: "FK_reservation_dict_reservation_status_status_id",
                        column: x => x.status_id,
                        principalTable: "dict_reservation_status",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reservation_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gate",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lift_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gate", x => x.id);
                    table.ForeignKey(
                        name: "FK_gate_lift_lift_id",
                        column: x => x.lift_id,
                        principalTable: "lift",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lift_schedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lift_id = table.Column<int>(type: "integer", nullable: true),
                    day_of_week = table.Column<int>(type: "integer", nullable: true),
                    opening_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    closing_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    season_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lift_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_lift_schedule_dict_season_season_id",
                        column: x => x.season_id,
                        principalTable: "dict_season",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_lift_schedule_lift_lift_id",
                        column: x => x.lift_id,
                        principalTable: "lift",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lift_trail",
                columns: table => new
                {
                    lift_id = table.Column<int>(type: "integer", nullable: false),
                    trail_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lift_trail", x => new { x.lift_id, x.trail_id });
                    table.ForeignKey(
                        name: "FK_lift_trail_lift_lift_id",
                        column: x => x.lift_id,
                        principalTable: "lift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lift_trail_trail_trail_id",
                        column: x => x.trail_id,
                        principalTable: "trail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trail_schedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    trail_id = table.Column<int>(type: "integer", nullable: true),
                    is_open = table.Column<bool>(type: "boolean", nullable: true),
                    closure_reason = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trail_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_trail_schedule_trail_trail_id",
                        column: x => x.trail_id,
                        principalTable: "trail",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ski_pass",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    card_id = table.Column<string>(type: "text", nullable: true),
                    tariff_id = table.Column<int>(type: "integer", nullable: true),
                    reservation_id = table.Column<int>(type: "integer", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    initial_rides = table.Column<int>(type: "integer", nullable: true),
                    remaining_rides = table.Column<int>(type: "integer", nullable: true),
                    block_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ski_pass", x => x.id);
                    table.ForeignKey(
                        name: "FK_ski_pass_card_card_id",
                        column: x => x.card_id,
                        principalTable: "card",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ski_pass_dict_pass_status_status_id",
                        column: x => x.status_id,
                        principalTable: "dict_pass_status",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ski_pass_reservation_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "reservation",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ski_pass_tariff_tariff_id",
                        column: x => x.tariff_id,
                        principalTable: "tariff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "transaction",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reservation_id = table.Column<int>(type: "integer", nullable: true),
                    cashier_id = table.Column<int>(type: "integer", nullable: true),
                    operation_type_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_transaction_cashier_cashier_id",
                        column: x => x.cashier_id,
                        principalTable: "cashier",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_dict_operation_type_operation_type_id",
                        column: x => x.operation_type_id,
                        principalTable: "dict_operation_type",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_reservation_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "reservation",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gate_scan",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    card_id = table.Column<string>(type: "text", nullable: true),
                    gate_id = table.Column<int>(type: "integer", nullable: true),
                    scan_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    time_blocked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verification_result_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gate_scan", x => x.id);
                    table.ForeignKey(
                        name: "FK_gate_scan_card_card_id",
                        column: x => x.card_id,
                        principalTable: "card",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_gate_scan_dict_verification_result_verification_result_id",
                        column: x => x.verification_result_id,
                        principalTable: "dict_verification_result",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_gate_scan_gate_gate_id",
                        column: x => x.gate_id,
                        principalTable: "gate",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_report_admin_id",
                table: "admin_report",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_report_report_type_id",
                table: "admin_report",
                column: "report_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_card_status_id",
                table: "card",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_card_user_id",
                table: "card",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gate_lift_id",
                table: "gate",
                column: "lift_id");

            migrationBuilder.CreateIndex(
                name: "IX_gate_scan_card_id",
                table: "gate_scan",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_gate_scan_gate_id",
                table: "gate_scan",
                column: "gate_id");

            migrationBuilder.CreateIndex(
                name: "IX_gate_scan_verification_result_id",
                table: "gate_scan",
                column: "verification_result_id");

            migrationBuilder.CreateIndex(
                name: "IX_lift_planner_id",
                table: "lift",
                column: "planner_id");

            migrationBuilder.CreateIndex(
                name: "IX_lift_status_id",
                table: "lift",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_lift_schedule_lift_id",
                table: "lift_schedule",
                column: "lift_id");

            migrationBuilder.CreateIndex(
                name: "IX_lift_schedule_season_id",
                table: "lift_schedule",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_lift_trail_trail_id",
                table: "lift_trail",
                column: "trail_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_status_id",
                table: "reservation",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_user_id",
                table: "reservation",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_report_cashier_id",
                table: "shift_report",
                column: "cashier_id");

            migrationBuilder.CreateIndex(
                name: "IX_ski_pass_card_id",
                table: "ski_pass",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_ski_pass_reservation_id",
                table: "ski_pass",
                column: "reservation_id");

            migrationBuilder.CreateIndex(
                name: "IX_ski_pass_status_id",
                table: "ski_pass",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_ski_pass_tariff_id",
                table: "ski_pass",
                column: "tariff_id");

            migrationBuilder.CreateIndex(
                name: "IX_tariff_pass_type_id",
                table: "tariff",
                column: "pass_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_tariff_season_id",
                table: "tariff",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_trail_difficulty_id",
                table: "trail",
                column: "difficulty_id");

            migrationBuilder.CreateIndex(
                name: "IX_trail_planner_id",
                table: "trail",
                column: "planner_id");

            migrationBuilder.CreateIndex(
                name: "IX_trail_status_id",
                table: "trail",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_trail_schedule_trail_id",
                table: "trail_schedule",
                column: "trail_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_cashier_id",
                table: "transaction",
                column: "cashier_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_operation_type_id",
                table: "transaction",
                column: "operation_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_reservation_id",
                table: "transaction",
                column: "reservation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_report");

            migrationBuilder.DropTable(
                name: "gate_scan");

            migrationBuilder.DropTable(
                name: "lift_schedule");

            migrationBuilder.DropTable(
                name: "lift_trail");

            migrationBuilder.DropTable(
                name: "shift_report");

            migrationBuilder.DropTable(
                name: "ski_pass");

            migrationBuilder.DropTable(
                name: "trail_schedule");

            migrationBuilder.DropTable(
                name: "transaction");

            migrationBuilder.DropTable(
                name: "administrator");

            migrationBuilder.DropTable(
                name: "dict_report_type");

            migrationBuilder.DropTable(
                name: "dict_verification_result");

            migrationBuilder.DropTable(
                name: "gate");

            migrationBuilder.DropTable(
                name: "card");

            migrationBuilder.DropTable(
                name: "dict_pass_status");

            migrationBuilder.DropTable(
                name: "tariff");

            migrationBuilder.DropTable(
                name: "trail");

            migrationBuilder.DropTable(
                name: "cashier");

            migrationBuilder.DropTable(
                name: "dict_operation_type");

            migrationBuilder.DropTable(
                name: "reservation");

            migrationBuilder.DropTable(
                name: "lift");

            migrationBuilder.DropTable(
                name: "dict_card_status");

            migrationBuilder.DropTable(
                name: "dict_pass_type");

            migrationBuilder.DropTable(
                name: "dict_season");

            migrationBuilder.DropTable(
                name: "dict_trail_difficulty");

            migrationBuilder.DropTable(
                name: "dict_trail_status");

            migrationBuilder.DropTable(
                name: "dict_reservation_status");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "dict_lift_status");

            migrationBuilder.DropTable(
                name: "trail_planner");
        }
    }
}
