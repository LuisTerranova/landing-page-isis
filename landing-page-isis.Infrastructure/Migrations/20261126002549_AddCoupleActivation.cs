using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddCoupleActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "couples",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "couples");
        }
    }
}
