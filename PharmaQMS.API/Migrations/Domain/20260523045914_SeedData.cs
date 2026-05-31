using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PharmaQMS.API.Migrations.Domain
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MasterRecipes",
                columns: new[] { "Id", "IsApproved", "RecipeName", "Version" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), true, "AMOX-500MG-CAP", "3.1" },
                    { new Guid("22222222-0000-0000-0000-000000000002"), true, "AMOX-250MG-CAP", "2.0" }
                });

            migrationBuilder.InsertData(
                table: "ProductionLines",
                columns: new[] { "Id", "IsActive", "LineName", "Location" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), true, "Line A", "Hall 1" },
                    { new Guid("11111111-0000-0000-0000-000000000002"), true, "Line B", "Hall 1" },
                    { new Guid("11111111-0000-0000-0000-000000000003"), true, "Line C", "Hall 2" }
                });

            migrationBuilder.InsertData(
                table: "MasterRecipeSteps",
                columns: new[] { "Id", "ExpectedFields", "IsCritical", "MasterRecipeId", "StepName", "StepNumber" },
                values: new object[,]
                {
                    { new Guid("33333333-0000-0000-0000-000000000001"), "Weight (kg)", true, new Guid("22222222-0000-0000-0000-000000000001"), "Weigh raw materials", 1 },
                    { new Guid("33333333-0000-0000-0000-000000000002"), "Mix time (min)", false, new Guid("22222222-0000-0000-0000-000000000001"), "Mixing", 2 },
                    { new Guid("33333333-0000-0000-0000-000000000003"), "Temperature (°C); Time (min)", true, new Guid("22222222-0000-0000-0000-000000000001"), "Granulation", 3 },
                    { new Guid("33333333-0000-0000-0000-000000000004"), "Moisture (%)", false, new Guid("22222222-0000-0000-0000-000000000001"), "Drying", 4 },
                    { new Guid("33333333-0000-0000-0000-000000000005"), "Fill weight (mg)", true, new Guid("22222222-0000-0000-0000-000000000001"), "Capsule filling", 5 },
                    { new Guid("33333333-0000-0000-0000-000000000006"), "Result", true, new Guid("22222222-0000-0000-0000-000000000001"), "In-process quality check", 6 },
                    { new Guid("33333333-0000-0000-0000-000000000007"), "Weight (kg)", true, new Guid("22222222-0000-0000-0000-000000000002"), "Weigh raw materials", 1 },
                    { new Guid("33333333-0000-0000-0000-000000000008"), "Mix time (min)", false, new Guid("22222222-0000-0000-0000-000000000002"), "Mixing", 2 },
                    { new Guid("33333333-0000-0000-0000-000000000009"), "Fill weight (mg)", true, new Guid("22222222-0000-0000-0000-000000000002"), "Capsule filling", 3 },
                    { new Guid("33333333-0000-0000-0000-000000000010"), "Result", true, new Guid("22222222-0000-0000-0000-000000000002"), "In-process quality check", 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "MasterRecipeSteps",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "ProductionLines",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "ProductionLines",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "ProductionLines",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "MasterRecipes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "MasterRecipes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000002"));
        }
    }
}
