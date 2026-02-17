using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AdjustmentsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "appointment_price",
                table: "appointments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "appointment_price",
                table: "appointments");
        }
    }
}
