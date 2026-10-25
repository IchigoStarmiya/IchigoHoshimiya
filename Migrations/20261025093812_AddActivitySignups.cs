using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace IchigoHoshimiya.Migrations
{
    /// <inheritdoc />
    public partial class AddActivitySignups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_signup",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    channel_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    message_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    slot_start_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    slot_end_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    close_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_siege_day = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_test = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_open = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_signup", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "activity_signup_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    activity_signup_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    choice = table.Column<int>(type: "int", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_signup_entry", x => x.id);
                    table.ForeignKey(
                        name: "FK_activity_signup_entry_activity_signup_activity_signup_id",
                        column: x => x.activity_signup_id,
                        principalTable: "activity_signup",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_activity_signup_is_open_close_at_utc",
                table: "activity_signup",
                columns: new[] { "is_open", "close_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_activity_signup_slot_start_utc",
                table: "activity_signup",
                column: "slot_start_utc");

            migrationBuilder.CreateIndex(
                name: "IX_activity_signup_entry_activity_signup_id_user_id",
                table: "activity_signup_entry",
                columns: new[] { "activity_signup_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_signup_entry");

            migrationBuilder.DropTable(
                name: "activity_signup");
        }
    }
}
