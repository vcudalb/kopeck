using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kopeck.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CreatedByName",
                table: "SavingsEntries",
                type: "TEXT",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SavingsEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "SavingsEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SavingsEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "SavingsEntries",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "SavingsEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Incomes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Incomes",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Incomes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Incomes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "Incomes",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Incomes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Expenses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Expenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "Expenses",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Expenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntryChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BudgetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ByName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Changes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntryChanges_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntryChanges_BudgetId_EntryId_At",
                table: "EntryChanges",
                columns: new[] { "BudgetId", "EntryId", "At" });

            // Entries added before this change: when they were entered isn't known, so use the date they are booked on.
            foreach (var table in new[] { "Expenses", "Incomes", "SavingsEntries" })
                migrationBuilder.Sql($"UPDATE {table} SET CreatedAt = Date || ' 00:00:00';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntryChanges");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SavingsEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "SavingsEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SavingsEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "SavingsEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "SavingsEntries");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Expenses");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByName",
                table: "SavingsEntries",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 80,
                oldNullable: true);
        }
    }
}
