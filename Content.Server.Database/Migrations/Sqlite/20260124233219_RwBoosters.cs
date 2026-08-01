using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RwBoosters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "amour_boosters",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ooc_color = table.Column<int>(type: "INTEGER", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amour_boosters", x => x.player_id);
                    table.ForeignKey(
                        name: "FK_amour_boosters_player_player_id1",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "amour_boosters");
        }
    }
}
