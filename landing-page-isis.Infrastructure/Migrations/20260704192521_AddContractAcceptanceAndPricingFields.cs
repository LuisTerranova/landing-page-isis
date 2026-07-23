using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddContractAcceptanceAndPricingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "professional_name", table: "contracts");

            migrationBuilder.AddColumn<string>(
                name: "acceptance_token",
                table: "contracts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "accepted_at",
                table: "contracts",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<decimal>(
                name: "package_price",
                table: "contracts",
                type: "numeric",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "acceptance_token", table: "contracts");

            migrationBuilder.DropColumn(name: "accepted_at", table: "contracts");

            migrationBuilder.DropColumn(name: "package_price", table: "contracts");

            migrationBuilder.AddColumn<string>(
                name: "professional_name",
                table: "contracts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true
            );
        }
    }
}
