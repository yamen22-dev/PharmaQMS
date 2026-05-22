using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaQMS.API.Migrations.Domain
{
    /// <inheritdoc />
    public partial class PurchaseOrderNumberAndAnalysisCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalysisCertificate",
                table: "Lots",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PurchaseOrderNumber",
                table: "Lots",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisCertificate",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderNumber",
                table: "Lots");
        }
    }
}
