using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace IchigoHoshimiya.Migrations
{
    /// <inheritdoc />
    public partial class AddEnergyCharacters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "energy_character",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    item_level = table.Column<int>(type: "int", nullable: true),
                    is_main = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    energy = table.Column<int>(type: "int", nullable: false),
                    extra_energy = table.Column<int>(type: "int", nullable: false),
                    nightmare_tickets = table.Column<int>(type: "int", nullable: false),
                    energy_updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    tickets_updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    energy_alert_sent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ticket_alert_sent = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_character", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_energy_character_user_id_name",
                table: "energy_character",
                columns: new[] { "user_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "energy_character");
        }
    }
}
