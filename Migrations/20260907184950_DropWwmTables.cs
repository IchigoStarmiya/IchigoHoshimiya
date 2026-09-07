using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IchigoHoshimiya.Migrations
{
    /// <inheritdoc />
    public partial class DropWwmTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Where Winds Meet support was removed; WwmDbContext and its migrations are gone,
            // so these tables are dropped here in child-first order and the orphaned history
            // row is cleared from the history table the remaining contexts share.
            string[] tables =
            [
                "wwm_gear_affix",
                "wwm_gear_base_attr",
                "wwm_gear_retone",
                "wwm_snapshot_skill",
                "wwm_snapshot_xinfa",
                "wwm_tracked_player",
                "wwm_gear",
                "wwm_player_snapshot",
                "wwm_player"
            ];

            foreach (var table in tables)
            {
                migrationBuilder.Sql($"DROP TABLE IF EXISTS `{table}`;");
            }

            migrationBuilder.Sql(
                "DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260731145751_AddWwmBuildSnapshots';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: the entities and the context that described these tables no longer exist.
        }
    }
}
