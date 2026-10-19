using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IchigoHoshimiya.Migrations
{
    /// <inheritdoc />
    public partial class TrackEnergyAlertLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "energy_alert_level",
                table: "energy_character",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ticket_alert_level",
                table: "energy_character",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Alerts were only ever sent at Critical, so carry those over to avoid repeat pings
            migrationBuilder.Sql(
                "UPDATE energy_character SET " +
                "energy_alert_level = IF(energy_alert_sent, 2, 0), " +
                "ticket_alert_level = IF(ticket_alert_sent, 2, 0);");

            migrationBuilder.DropColumn(
                name: "energy_alert_sent",
                table: "energy_character");

            migrationBuilder.DropColumn(
                name: "ticket_alert_sent",
                table: "energy_character");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "energy_alert_level",
                table: "energy_character");

            migrationBuilder.DropColumn(
                name: "ticket_alert_level",
                table: "energy_character");

            migrationBuilder.AddColumn<bool>(
                name: "energy_alert_sent",
                table: "energy_character",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ticket_alert_sent",
                table: "energy_character",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
