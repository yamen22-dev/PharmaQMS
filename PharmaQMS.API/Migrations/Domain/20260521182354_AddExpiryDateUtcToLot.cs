using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaQMS.API.Migrations.Domain
{
    /// <inheritdoc />
    public partial class AddExpiryDateUtcToLot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lots_LotNumber",
                table: "Lots");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDateUtc",
                table: "Lots",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Lots_RawMaterialId_LotNumber",
                table: "Lots",
                columns: new[] { "RawMaterialId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lots_ReceivedDateUtc",
                table: "Lots",
                column: "ReceivedDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_Status",
                table: "Lots",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lots_RawMaterialId_LotNumber",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_ReceivedDateUtc",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_Status",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "ExpiryDateUtc",
                table: "Lots");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_LotNumber",
                table: "Lots",
                column: "LotNumber",
                unique: true);
        }
    }
}
