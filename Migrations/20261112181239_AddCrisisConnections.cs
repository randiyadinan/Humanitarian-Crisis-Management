using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanitarianAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCrisisConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CrisisEventId",
                table: "Volunteers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShelterId",
                table: "Volunteers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CrisisEventId",
                table: "Shelters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CrisisEventId",
                table: "Resources",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShelterId",
                table: "Resources",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CrisisEventId",
                table: "Donations",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrisisEventId",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "ShelterId",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "CrisisEventId",
                table: "Shelters");

            migrationBuilder.DropColumn(
                name: "CrisisEventId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ShelterId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "CrisisEventId",
                table: "Donations");
        }
    }
}
