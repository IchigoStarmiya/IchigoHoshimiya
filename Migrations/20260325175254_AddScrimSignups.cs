using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace IchigoHoshimiya.Migrations
{
    /// <inheritdoc />
    public partial class AddScrimSignups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scrim_signup",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    channel_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    message_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    created_by_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    is_open = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrim_signup", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scrim_signup_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    scrim_signup_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    role = table.Column<int>(type: "int", nullable: false),
                    available_days = table.Column<int>(type: "int", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrim_signup_entry", x => x.id);
                    table.ForeignKey(
                        name: "FK_scrim_signup_entry_scrim_signup_scrim_signup_id",
                        column: x => x.scrim_signup_id,
                        principalTable: "scrim_signup",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_scrim_signup_created_at_utc",
                table: "scrim_signup",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_scrim_signup_created_by_id",
                table: "scrim_signup",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_scrim_signup_message_id",
                table: "scrim_signup",
                column: "message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scrim_signup_entry_scrim_signup_id_user_id",
                table: "scrim_signup_entry",
                columns: new[] { "scrim_signup_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scrim_signup_entry_user_id",
                table: "scrim_signup_entry",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scrim_signup_entry");

            migrationBuilder.DropTable(
                name: "scrim_signup");
        }
    }
}
