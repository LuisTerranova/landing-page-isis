using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddCoupleFieldsToContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "couple_name",
                table: "contracts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true
            );

            migrationBuilder.AddColumn<DateOnly>(
                name: "patient2_birth_date",
                table: "contracts",
                type: "date",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "patient2_cpf",
                table: "contracts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "patient2_cpf_hash",
                table: "contracts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "patient2_email",
                table: "contracts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "patient2_name",
                table: "contracts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "patient2_phone",
                table: "contracts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "patient2_state",
                table: "contracts",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "couple_name", table: "contracts");

            migrationBuilder.DropColumn(name: "patient2_birth_date", table: "contracts");

            migrationBuilder.DropColumn(name: "patient2_cpf", table: "contracts");

            migrationBuilder.DropColumn(name: "patient2_cpf_hash", table: "contracts");

            migrationBuilder.DropColumn(name: "patient2_email", table: "contracts");

            migrationBuilder.DropColumn(name: "patient2_name", table: "contracts");

            migrationBuilder.DropColumn(name: "patient2_phone", table: "contracts");

            migrationBuilder.DropColumn(name: "patient2_state", table: "contracts");
        }
    }
}
