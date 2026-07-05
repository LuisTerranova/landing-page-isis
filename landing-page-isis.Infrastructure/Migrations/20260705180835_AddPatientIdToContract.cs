using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientIdToContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "patient_id",
                table: "contracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_contracts_patient_id",
                table: "contracts",
                column: "patient_id",
                unique: true,
                filter: "\"patient_id\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_contracts_patients_patient_id",
                table: "contracts",
                column: "patient_id",
                principalTable: "patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contracts_patients_patient_id",
                table: "contracts");

            migrationBuilder.DropIndex(
                name: "IX_contracts_patient_id",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "patient_id",
                table: "contracts");
        }
    }
}
