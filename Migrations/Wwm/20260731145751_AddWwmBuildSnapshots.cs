using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace IchigoHoshimiya.Migrations.Wwm
{
    /// <inheritdoc />
    public partial class AddWwmBuildSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wwm_player",
                columns: table => new
                {
                    number_id = table.Column<long>(type: "bigint", nullable: false),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    region = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    server = table.Column<int>(type: "int", nullable: false),
                    account_created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    first_seen_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_seen_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wwm_player", x => x.number_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wwm_tracked_player",
                columns: table => new
                {
                    number_id = table.Column<long>(type: "bigint", nullable: false),
                    label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    added_by_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    added_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_checked_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_success_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_build_change_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_error = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wwm_tracked_player", x => x.number_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wwm_player_snapshot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    number_id = table.Column<long>(type: "bigint", nullable: false),
                    fetched_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    data_updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    build_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    was_cached = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    was_stale = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    lookup_job_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    lookup_type = table.Column<int>(type: "int", nullable: false),
                    lookup_query = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    server = table.Column<int>(type: "int", nullable: false),
                    region = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    oversea_tag = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    guild_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    club_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    discord_account_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    level = table.Column<int>(type: "int", nullable: false),
                    school = table.Column<int>(type: "int", nullable: true),
                    is_master = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    build_power = table.Column<int>(type: "int", nullable: true),
                    max_xiuwei_kungfu = table.Column<int>(type: "int", nullable: true),
                    celestial_pulls = table.Column<int>(type: "int", nullable: true),
                    solemn_pulls = table.Column<int>(type: "int", nullable: true),
                    harmonic_pulls = table.Column<int>(type: "int", nullable: true),
                    other_legend_pulls = table.Column<int>(type: "int", nullable: true),
                    account_created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    login_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    logout_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    online_time_seconds = table.Column<double>(type: "double", nullable: true),
                    is_online = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    has_pvp_data = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    total_matches = table.Column<int>(type: "int", nullable: true),
                    wins = table.Column<int>(type: "int", nullable: true),
                    losses = table.Column<int>(type: "int", nullable: true),
                    win_rate = table.Column<double>(type: "double", nullable: true),
                    win_rate_percent = table.Column<double>(type: "double", nullable: true),
                    winning_streak = table.Column<int>(type: "int", nullable: true),
                    pvp_score = table.Column<int>(type: "int", nullable: true),
                    surface_score = table.Column<int>(type: "int", nullable: true),
                    pvp_grade = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    pvp_small_grade = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    pvp_max_grade = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    pvp_max_small_grade = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    pvp_season_id = table.Column<int>(type: "int", nullable: true),
                    lunjian_grade = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    lunjian_small_grade = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    max_wuwo_score = table.Column<double>(type: "double", nullable: true),
                    lunjian_max_streak = table.Column<int>(type: "int", nullable: true),
                    lunjian_total = table.Column<int>(type: "int", nullable: true),
                    arena_rank = table.Column<int>(type: "int", nullable: true),
                    main_weapon_id = table.Column<int>(type: "int", nullable: true),
                    main_weapon_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    sub_weapon_id = table.Column<int>(type: "int", nullable: true),
                    sub_weapon_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    gameplay_trail_json = table.Column<string>(type: "TEXT", nullable: true),
                    current_scenario_json = table.Column<string>(type: "TEXT", nullable: true),
                    raw_base_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wwm_player_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "FK_wwm_player_snapshot_wwm_player_number_id",
                        column: x => x.number_id,
                        principalTable: "wwm_player",
                        principalColumn: "number_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wwm_gear",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    snapshot_id = table.Column<long>(type: "bigint", nullable: false),
                    slot_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    slot_ord = table.Column<int>(type: "int", nullable: false),
                    item_no = table.Column<long>(type: "bigint", nullable: false),
                    item_index = table.Column<int>(type: "int", nullable: true),
                    durability = table.Column<int>(type: "int", nullable: true),
                    suit = table.Column<int>(type: "int", nullable: true),
                    suit_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    retoned = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    retone_no = table.Column<long>(type: "bigint", nullable: true),
                    retone_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    retone_count = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wwm_gear", x => x.id);
                    table.ForeignKey(
                        name: "FK_wwm_gear_wwm_player_snapshot_snapshot_id",
                        column: x => x.snapshot_id,
                        principalTable: "wwm_player_snapshot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wwm_snapshot_skill",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    snapshot_id = table.Column<long>(type: "bigint", nullable: false),
                    slot = table.Column<int>(type: "int", nullable: false),
                    skill_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wwm_snapshot_skill", x => x.id);
                    table.ForeignKey(
                        name: "FK_wwm_snapshot_skill_wwm_player_snapshot_snapshot_id",
                        column: x => x.snapshot_id,
                        principalTable: "wwm_player_snapshot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wwm_snapshot_xinfa",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    snapshot_id = table.Column<long>(type: "bigint", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    xinfa_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wwm_snapshot_xinfa", x => x.id);
                    table.ForeignKey(
                        name: "FK_wwm_snapshot_xinfa_wwm_player_snapshot_snapshot_id",
                        column: x => x.snapshot_id,
                        principalTable: "wwm_player_snapshot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wwm_gear_affix",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    gear_id = table.Column<long>(type: "bigint", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    affix_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    value = table.Column<double>(type: "double", nullable: false),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    active = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    display_value = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wwm_gear_affix", x => x.id);
                    table.ForeignKey(
                        name: "FK_wwm_gear_affix_wwm_gear_gear_id",
                        column: x => x.gear_id,
                        principalTable: "wwm_gear",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wwm_gear_base_attr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    gear_id = table.Column<long>(type: "bigint", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    raw_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    value = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wwm_gear_base_attr", x => x.id);
                    table.ForeignKey(
                        name: "FK_wwm_gear_base_attr_wwm_gear_gear_id",
                        column: x => x.gear_id,
                        principalTable: "wwm_gear",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wwm_gear_retone",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    gear_id = table.Column<long>(type: "bigint", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    retone_no = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wwm_gear_retone", x => x.id);
                    table.ForeignKey(
                        name: "FK_wwm_gear_retone_wwm_gear_gear_id",
                        column: x => x.gear_id,
                        principalTable: "wwm_gear",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_gear_item_no",
                table: "wwm_gear",
                column: "item_no");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_gear_snapshot_id_slot_ord",
                table: "wwm_gear",
                columns: new[] { "snapshot_id", "slot_ord" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wwm_gear_suit_name",
                table: "wwm_gear",
                column: "suit_name");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_gear_affix_affix_id",
                table: "wwm_gear_affix",
                column: "affix_id");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_gear_affix_gear_id_ordinal",
                table: "wwm_gear_affix",
                columns: new[] { "gear_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wwm_gear_affix_type",
                table: "wwm_gear_affix",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_gear_base_attr_gear_id_ordinal",
                table: "wwm_gear_base_attr",
                columns: new[] { "gear_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wwm_gear_base_attr_raw_name",
                table: "wwm_gear_base_attr",
                column: "raw_name");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_gear_retone_gear_id_ordinal",
                table: "wwm_gear_retone",
                columns: new[] { "gear_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wwm_player_external_id",
                table: "wwm_player",
                column: "external_id");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_player_name",
                table: "wwm_player",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_player_snapshot_fetched_at_utc",
                table: "wwm_player_snapshot",
                column: "fetched_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_player_snapshot_guild_name",
                table: "wwm_player_snapshot",
                column: "guild_name");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_player_snapshot_lookup_job_id",
                table: "wwm_player_snapshot",
                column: "lookup_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_player_snapshot_number_id_build_hash",
                table: "wwm_player_snapshot",
                columns: new[] { "number_id", "build_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_wwm_player_snapshot_number_id_fetched_at_utc",
                table: "wwm_player_snapshot",
                columns: new[] { "number_id", "fetched_at_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wwm_snapshot_skill_skill_id",
                table: "wwm_snapshot_skill",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_snapshot_skill_snapshot_id_slot",
                table: "wwm_snapshot_skill",
                columns: new[] { "snapshot_id", "slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wwm_snapshot_xinfa_snapshot_id_ordinal",
                table: "wwm_snapshot_xinfa",
                columns: new[] { "snapshot_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wwm_snapshot_xinfa_xinfa_id",
                table: "wwm_snapshot_xinfa",
                column: "xinfa_id");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_tracked_player_enabled",
                table: "wwm_tracked_player",
                column: "enabled");

            migrationBuilder.CreateIndex(
                name: "IX_wwm_tracked_player_last_checked_at_utc",
                table: "wwm_tracked_player",
                column: "last_checked_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wwm_gear_affix");

            migrationBuilder.DropTable(
                name: "wwm_gear_base_attr");

            migrationBuilder.DropTable(
                name: "wwm_gear_retone");

            migrationBuilder.DropTable(
                name: "wwm_snapshot_skill");

            migrationBuilder.DropTable(
                name: "wwm_snapshot_xinfa");

            migrationBuilder.DropTable(
                name: "wwm_tracked_player");

            migrationBuilder.DropTable(
                name: "wwm_gear");

            migrationBuilder.DropTable(
                name: "wwm_player_snapshot");

            migrationBuilder.DropTable(
                name: "wwm_player");
        }
    }
}
