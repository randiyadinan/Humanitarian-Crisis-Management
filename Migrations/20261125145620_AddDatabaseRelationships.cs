using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanitarianAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_CrisisEventId",
                table: "Volunteers",
                column: "CrisisEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_ShelterId",
                table: "Volunteers",
                column: "ShelterId");

            migrationBuilder.CreateIndex(
                name: "IX_Shelters_CrisisEventId",
                table: "Shelters",
                column: "CrisisEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CrisisEventId",
                table: "Resources",
                column: "CrisisEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ShelterId",
                table: "Resources",
                column: "ShelterId");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_CrisisEventId",
                table: "Donations",
                column: "CrisisEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AIRecommendations_RequestId",
                table: "AIRecommendations",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AIRecommendations_AssistanceRequests_RequestId",
                table: "AIRecommendations",
                column: "RequestId",
                principalTable: "AssistanceRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_CrisisEvents_CrisisEventId",
                table: "Donations",
                column: "CrisisEventId",
                principalTable: "CrisisEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_CrisisEvents_CrisisEventId",
                table: "Resources",
                column: "CrisisEventId",
                principalTable: "CrisisEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Shelters_ShelterId",
                table: "Resources",
                column: "ShelterId",
                principalTable: "Shelters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Shelters_CrisisEvents_CrisisEventId",
                table: "Shelters",
                column: "CrisisEventId",
                principalTable: "CrisisEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Volunteers_CrisisEvents_CrisisEventId",
                table: "Volunteers",
                column: "CrisisEventId",
                principalTable: "CrisisEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Volunteers_Shelters_ShelterId",
                table: "Volunteers",
                column: "ShelterId",
                principalTable: "Shelters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AIRecommendations_AssistanceRequests_RequestId",
                table: "AIRecommendations");

            migrationBuilder.DropForeignKey(
                name: "FK_Donations_CrisisEvents_CrisisEventId",
                table: "Donations");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_CrisisEvents_CrisisEventId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Shelters_ShelterId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Shelters_CrisisEvents_CrisisEventId",
                table: "Shelters");

            migrationBuilder.DropForeignKey(
                name: "FK_Volunteers_CrisisEvents_CrisisEventId",
                table: "Volunteers");

            migrationBuilder.DropForeignKey(
                name: "FK_Volunteers_Shelters_ShelterId",
                table: "Volunteers");

            migrationBuilder.DropIndex(
                name: "IX_Volunteers_CrisisEventId",
                table: "Volunteers");

            migrationBuilder.DropIndex(
                name: "IX_Volunteers_ShelterId",
                table: "Volunteers");

            migrationBuilder.DropIndex(
                name: "IX_Shelters_CrisisEventId",
                table: "Shelters");

            migrationBuilder.DropIndex(
                name: "IX_Resources_CrisisEventId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_ShelterId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Donations_CrisisEventId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_AIRecommendations_RequestId",
                table: "AIRecommendations");
        }
    }
}
