using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanitarianAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMember4FieldsAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedTask",
                table: "Volunteers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Specialisation",
                table: "Volunteers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Shelters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AllocatedQuantity",
                table: "Resources",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Resources",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Populate the new fields for existing rows so Members 1-3 data remains usable.
            migrationBuilder.Sql(
                "UPDATE \"Shelters\" SET \"Status\" = CASE " +
                "WHEN \"Capacity\" > 0 AND \"CurrentOccupancy\" >= \"Capacity\" THEN 'Full' " +
                "ELSE 'Open' END WHERE \"Status\" = '' OR \"Status\" IS NULL;");

            migrationBuilder.Sql(
                "UPDATE \"Resources\" SET \"Category\" = 'Uncategorised' " +
                "WHERE \"Category\" = '' OR \"Category\" IS NULL;");

            migrationBuilder.Sql(
                "UPDATE \"Volunteers\" SET \"Specialisation\" = 'General Relief' " +
                "WHERE \"Specialisation\" = '' OR \"Specialisation\" IS NULL;");

            migrationBuilder.CreateTable(
                name: "ShelterOccupants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShelterId = table.Column<int>(type: "INTEGER", nullable: false),
                    HouseholdName = table.Column<string>(type: "TEXT", nullable: false),
                    PersonCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelterOccupants", x => x.Id);
                    table.CheckConstraint("CK_ShelterOccupants_PersonCount_Positive", "\"PersonCount\" > 0");
                    table.ForeignKey(
                        name: "FK_ShelterOccupants_Shelters_ShelterId",
                        column: x => x.ShelterId,
                        principalTable: "Shelters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_Status",
                table: "Volunteers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Shelters_Status",
                table: "Shelters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Status_Category",
                table: "Resources",
                columns: new[] { "Status", "Category" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Resources_AllocatedQuantity_NonNegative",
                table: "Resources",
                sql: "\"AllocatedQuantity\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Resources_AllocatedQuantity_WithinQuantity",
                table: "Resources",
                sql: "\"AllocatedQuantity\" <= \"Quantity\"");

            migrationBuilder.CreateIndex(
                name: "IX_ShelterOccupants_ShelterId",
                table: "ShelterOccupants",
                column: "ShelterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShelterOccupants");

            migrationBuilder.DropIndex(
                name: "IX_Volunteers_Status",
                table: "Volunteers");

            migrationBuilder.DropIndex(
                name: "IX_Shelters_Status",
                table: "Shelters");

            migrationBuilder.DropIndex(
                name: "IX_Resources_Status_Category",
                table: "Resources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Resources_AllocatedQuantity_NonNegative",
                table: "Resources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Resources_AllocatedQuantity_WithinQuantity",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "AssignedTask",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "Specialisation",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Shelters");

            migrationBuilder.DropColumn(
                name: "AllocatedQuantity",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Resources");
        }
    }
}
