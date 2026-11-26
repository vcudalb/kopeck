using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kopeck.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsAndCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "IncomeStreams",
                type: "TEXT",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            // Existing streams were implicitly in the budget's currency; make that explicit.
            migrationBuilder.Sql(
                "UPDATE IncomeStreams SET Currency = (SELECT b.Currency FROM Budgets b WHERE b.Id = IncomeStreams.BudgetId);");

            migrationBuilder.CreateTable(
                name: "SavingsPots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BudgetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Target = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsPots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingsPots_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavingsEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BudgetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedByName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingsEntries_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavingsEntries_SavingsPots_PotId",
                        column: x => x.PotId,
                        principalTable: "SavingsPots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavingsEntries_BudgetId_Date",
                table: "SavingsEntries",
                columns: new[] { "BudgetId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_SavingsEntries_PotId",
                table: "SavingsEntries",
                column: "PotId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsPots_BudgetId_Name",
                table: "SavingsPots",
                columns: new[] { "BudgetId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavingsEntries");

            migrationBuilder.DropTable(
                name: "SavingsPots");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "IncomeStreams");
        }
    }
}
