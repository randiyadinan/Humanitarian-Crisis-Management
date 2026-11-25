using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanitarianAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Shelters_Capacity_NonNegative",
                table: "Shelters",
                sql: "\"Capacity\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shelters_Occupancy_NonNegative",
                table: "Shelters",
                sql: "\"CurrentOccupancy\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shelters_Occupancy_WithinCapacity",
                table: "Shelters",
                sql: "\"CurrentOccupancy\" <= \"Capacity\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Resources_Quantity_NonNegative",
                table: "Resources",
                sql: "\"Quantity\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Shelters_Capacity_NonNegative",
                table: "Shelters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shelters_Occupancy_NonNegative",
                table: "Shelters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shelters_Occupancy_WithinCapacity",
                table: "Shelters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Resources_Quantity_NonNegative",
                table: "Resources");
        }
    }
}
