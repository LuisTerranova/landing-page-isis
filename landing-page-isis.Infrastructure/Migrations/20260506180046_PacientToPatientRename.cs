using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class PacientToPatientRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointment_packages_pacients_pacient_id",
                table: "appointment_packages");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_pacients_PacientId",
                table: "appointments");

            migrationBuilder.RenameColumn(
                name: "pacient_id",
                table: "appointment_packages",
                newName: "patient_id");

            migrationBuilder.RenameColumn(
                name: "PacientId",
                table: "appointments",
                newName: "PatientId");

            migrationBuilder.DropPrimaryKey(
                name: "PK_pacients",
                table: "pacients");

            migrationBuilder.RenameTable(
                name: "pacients",
                newName: "patients");

            migrationBuilder.AddPrimaryKey(
                name: "PK_patients",
                table: "patients",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_appointment_packages_patients_patient_id",
                table: "appointment_packages",
                column: "patient_id",
                principalTable: "patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_patients_PatientId",
                table: "appointments",
                column: "PatientId",
                principalTable: "patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointment_packages_patients_patient_id",
                table: "appointment_packages");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_patients_PatientId",
                table: "appointments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_patients",
                table: "patients");

            migrationBuilder.RenameTable(
                name: "patients",
                newName: "pacients");

            migrationBuilder.RenameColumn(
                name: "patient_id",
                table: "appointment_packages",
                newName: "pacient_id");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "appointments",
                newName: "PacientId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_pacients",
                table: "pacients",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_appointment_packages_pacients_pacient_id",
                table: "appointment_packages",
                column: "patient_id",
                principalTable: "pacients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_pacients_PacientId",
                table: "appointments",
                column: "PatientId",
                principalTable: "pacients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
