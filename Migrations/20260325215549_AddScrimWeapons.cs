using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IchigoHoshimiya.Migrations
{
    /// <inheritdoc />
    public partial class AddScrimWeapons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "weapon",
                table: "scrim_signup_entry",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "weapon",
                table: "scrim_signup_entry");
        }
    }
}
