using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IchigoHoshimiya.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameToScrimSignupEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "username",
                table: "scrim_signup_entry",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "username",
                table: "scrim_signup_entry");
        }
    }
}
