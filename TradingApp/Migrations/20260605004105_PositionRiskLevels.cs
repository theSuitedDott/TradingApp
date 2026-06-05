using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingApp.Migrations
{
    /// <inheritdoc />
    public partial class PositionRiskLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StopLossPrice",
                table: "positions",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TakeProfitPrice",
                table: "positions",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopLossPrice",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "TakeProfitPrice",
                table: "positions");
        }
    }
}
