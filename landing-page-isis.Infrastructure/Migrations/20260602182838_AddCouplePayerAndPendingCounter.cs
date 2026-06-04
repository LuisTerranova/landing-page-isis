using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddCouplePayerAndPendingCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payer_cpf",
                table: "patients",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "payer_name",
                table: "patients",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "couple_id",
                table: "appointments",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "payer_cpf",
                table: "appointments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "payer_name",
                table: "appointments",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "couple_id",
                table: "appointment_packages",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "payer_cpf",
                table: "appointment_packages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "payer_name",
                table: "appointment_packages",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "couples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                    patient1_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient2_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payer_name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: true
                    ),
                    payer_cpf = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: true
                    ),
                    policy_signed = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_couples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_couples_patients_patient1_id",
                        column: x => x.patient1_id,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_couples_patients_patient2_id",
                        column: x => x.patient2_id,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_appointments_couple_id",
                table: "appointments",
                column: "couple_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_appointment_packages_couple_id",
                table: "appointment_packages",
                column: "couple_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_couples_patient1_id",
                table: "couples",
                column: "patient1_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_couples_patient2_id",
                table: "couples",
                column: "patient2_id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_appointment_packages_couples_couple_id",
                table: "appointment_packages",
                column: "couple_id",
                principalTable: "couples",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_couples_couple_id",
                table: "appointments",
                column: "couple_id",
                principalTable: "couples",
                principalColumn: "Id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointment_packages_couples_couple_id",
                table: "appointment_packages"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_couples_couple_id",
                table: "appointments"
            );

            migrationBuilder.DropTable(name: "couples");

            migrationBuilder.DropIndex(name: "IX_appointments_couple_id", table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointment_packages_couple_id",
                table: "appointment_packages"
            );

            migrationBuilder.DropColumn(name: "payer_cpf", table: "patients");

            migrationBuilder.DropColumn(name: "payer_name", table: "patients");

            migrationBuilder.DropColumn(name: "couple_id", table: "appointments");

            migrationBuilder.DropColumn(name: "payer_cpf", table: "appointments");

            migrationBuilder.DropColumn(name: "payer_name", table: "appointments");

            migrationBuilder.DropColumn(name: "couple_id", table: "appointment_packages");

            migrationBuilder.DropColumn(name: "payer_cpf", table: "appointment_packages");

            migrationBuilder.DropColumn(name: "payer_name", table: "appointment_packages");
        }
    }
}
