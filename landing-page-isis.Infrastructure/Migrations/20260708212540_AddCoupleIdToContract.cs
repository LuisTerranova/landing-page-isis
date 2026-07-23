using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddCoupleIdToContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "couple_id",
                table: "contracts",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_contracts_couple_id",
                table: "contracts",
                column: "couple_id",
                unique: true,
                filter: "\"couple_id\" IS NOT NULL"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_contracts_couples_couple_id",
                table: "contracts",
                column: "couple_id",
                principalTable: "couples",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contracts_couples_couple_id",
                table: "contracts"
            );

            migrationBuilder.DropIndex(name: "IX_contracts_couple_id", table: "contracts");

            migrationBuilder.DropColumn(name: "couple_id", table: "contracts");
        }
    }
}
