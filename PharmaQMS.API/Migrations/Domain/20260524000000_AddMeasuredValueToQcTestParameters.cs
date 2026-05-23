using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PharmaQMS.API.Data;

#nullable disable

namespace PharmaQMS.API.Migrations.Domain;

[DbContext(typeof(DomainDbContext))]
[Migration("20260524000000_AddMeasuredValueToQcTestParameters")]
public partial class AddMeasuredValueToQcTestParameters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "MeasuredValue",
            table: "QcTestParameters",
            type: "decimal(18,4)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MeasuredValue",
            table: "QcTestParameters");
    }
}