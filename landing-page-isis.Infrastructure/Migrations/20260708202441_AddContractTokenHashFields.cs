using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddContractTokenHashFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cpf_hash",
                table: "patients",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "patient_cpf_hash",
                table: "contracts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "token_generated_at",
                table: "contracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_patients_cpf_hash",
                table: "patients",
                column: "cpf_hash",
                unique: true,
                filter: "\"cpf_hash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_acceptance_token",
                table: "contracts",
                column: "acceptance_token",
                unique: true,
                filter: "\"acceptance_token\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_patient_cpf_hash",
                table: "contracts",
                column: "patient_cpf_hash",
                unique: true,
                filter: "\"patient_cpf_hash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_patients_cpf_hash",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "IX_contracts_acceptance_token",
                table: "contracts");

            migrationBuilder.DropIndex(
                name: "IX_contracts_patient_cpf_hash",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "cpf_hash",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "patient_cpf_hash",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "token_generated_at",
                table: "contracts");
        }
    }
}
