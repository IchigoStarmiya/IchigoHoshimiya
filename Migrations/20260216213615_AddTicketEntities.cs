using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace IchigoHoshimiya.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackedTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    TicketName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsClosed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedTickets", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TicketMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    DiscordMessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    TrackedTicketId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    AuthorName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    AttachmentUrls = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketMessages_TrackedTickets_TrackedTicketId",
                        column: x => x.TrackedTicketId,
                        principalTable: "TrackedTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_ChannelId",
                table: "TicketMessages",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_ChannelId_DiscordMessageId",
                table: "TicketMessages",
                columns: new[] { "ChannelId", "DiscordMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_DiscordMessageId",
                table: "TicketMessages",
                column: "DiscordMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_TrackedTicketId",
                table: "TicketMessages",
                column: "TrackedTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedTickets_ChannelId",
                table: "TrackedTickets",
                column: "ChannelId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketMessages");

            migrationBuilder.DropTable(
                name: "TrackedTickets");
        }
    }
}
