using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaQMS.API.Migrations.Domain
{
    /// <inheritdoc />
    public partial class BmrStepLotLinkAndMasterRecipeStepProductionLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterRecipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RecipeName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsApproved = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterRecipes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LineName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionLines", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MasterRecipeSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MasterRecipeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCritical = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExpectedFields = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterRecipeSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterRecipeSteps_MasterRecipes_MasterRecipeId",
                        column: x => x.MasterRecipeId,
                        principalTable: "MasterRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Bmrs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BatchNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MasterRecipeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BatchSize = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CreatedById = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MasterRecipeId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ProductionLineId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bmrs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bmrs_MasterRecipes_MasterRecipeId",
                        column: x => x.MasterRecipeId,
                        principalTable: "MasterRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bmrs_MasterRecipes_MasterRecipeId1",
                        column: x => x.MasterRecipeId1,
                        principalTable: "MasterRecipes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bmrs_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bmrs_ProductionLines_ProductionLineId1",
                        column: x => x.ProductionLineId1,
                        principalTable: "ProductionLines",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BmrLotLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BmrId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    BmrId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BmrLotLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BmrLotLinks_Bmrs_BmrId",
                        column: x => x.BmrId,
                        principalTable: "Bmrs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BmrLotLinks_Bmrs_BmrId1",
                        column: x => x.BmrId1,
                        principalTable: "Bmrs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BmrLotLinks_Lots_LotId",
                        column: x => x.LotId,
                        principalTable: "Lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BmrSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BmrId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MasterRecipeStepId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    EnteredData = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnteredById = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnteredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VerifiedById = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VerifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeviationNote = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MasterRecipeStepId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BmrSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BmrSteps_Bmrs_BmrId",
                        column: x => x.BmrId,
                        principalTable: "Bmrs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BmrSteps_MasterRecipeSteps_MasterRecipeStepId",
                        column: x => x.MasterRecipeStepId,
                        principalTable: "MasterRecipeSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BmrSteps_MasterRecipeSteps_MasterRecipeStepId1",
                        column: x => x.MasterRecipeStepId1,
                        principalTable: "MasterRecipeSteps",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BmrLotLinks_BmrId",
                table: "BmrLotLinks",
                column: "BmrId");

            migrationBuilder.CreateIndex(
                name: "IX_BmrLotLinks_BmrId_LotId",
                table: "BmrLotLinks",
                columns: new[] { "BmrId", "LotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BmrLotLinks_BmrId1",
                table: "BmrLotLinks",
                column: "BmrId1");

            migrationBuilder.CreateIndex(
                name: "IX_BmrLotLinks_LotId",
                table: "BmrLotLinks",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Bmrs_BatchNumber",
                table: "Bmrs",
                column: "BatchNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Bmrs_CreatedById",
                table: "Bmrs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Bmrs_MasterRecipeId",
                table: "Bmrs",
                column: "MasterRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Bmrs_MasterRecipeId1",
                table: "Bmrs",
                column: "MasterRecipeId1");

            migrationBuilder.CreateIndex(
                name: "IX_Bmrs_ProductionLineId",
                table: "Bmrs",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Bmrs_ProductionLineId1",
                table: "Bmrs",
                column: "ProductionLineId1");

            migrationBuilder.CreateIndex(
                name: "IX_BmrSteps_BmrId",
                table: "BmrSteps",
                column: "BmrId");

            migrationBuilder.CreateIndex(
                name: "IX_BmrSteps_EnteredById",
                table: "BmrSteps",
                column: "EnteredById");

            migrationBuilder.CreateIndex(
                name: "IX_BmrSteps_MasterRecipeStepId",
                table: "BmrSteps",
                column: "MasterRecipeStepId");

            migrationBuilder.CreateIndex(
                name: "IX_BmrSteps_MasterRecipeStepId1",
                table: "BmrSteps",
                column: "MasterRecipeStepId1");

            migrationBuilder.CreateIndex(
                name: "IX_BmrSteps_VerifiedById",
                table: "BmrSteps",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_MasterRecipes_RecipeName",
                table: "MasterRecipes",
                column: "RecipeName");

            migrationBuilder.CreateIndex(
                name: "IX_MasterRecipeSteps_MasterRecipeId",
                table: "MasterRecipeSteps",
                column: "MasterRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterRecipeSteps_MasterRecipeId_StepNumber",
                table: "MasterRecipeSteps",
                columns: new[] { "MasterRecipeId", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_LineName",
                table: "ProductionLines",
                column: "LineName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BmrLotLinks");

            migrationBuilder.DropTable(
                name: "BmrSteps");

            migrationBuilder.DropTable(
                name: "Bmrs");

            migrationBuilder.DropTable(
                name: "MasterRecipeSteps");

            migrationBuilder.DropTable(
                name: "ProductionLines");

            migrationBuilder.DropTable(
                name: "MasterRecipes");
        }
    }
}
