using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class BarkSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "bark_pause",
                table: "profile",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)127);

            migrationBuilder.AddColumn<byte>(
                name: "bark_pitch",
                table: "profile",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)127);

            migrationBuilder.AddColumn<byte>(
                name: "bark_pitch_variance",
                table: "profile",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)127);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bark_pause",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "bark_pitch",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "bark_pitch_variance",
                table: "profile");
        }
    }
}
