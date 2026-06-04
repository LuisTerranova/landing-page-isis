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
            // Drop old FKs referencing pacients table
            migrationBuilder.DropForeignKey(
                name: "FK_appointment_packages_pacients_patient_id",
                table: "appointment_packages"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_pacients_PatientId",
                table: "appointments"
            );

            // Rename table pacients -> patients
            migrationBuilder.DropPrimaryKey(name: "PK_pacients", table: "pacients");

            migrationBuilder.RenameTable(name: "pacients", newName: "patients");

            migrationBuilder.AddPrimaryKey(name: "PK_patients", table: "patients", column: "Id");

            // Recreate FKs pointing to new patients table
            migrationBuilder.AddForeignKey(
                name: "FK_appointment_packages_patients_patient_id",
                table: "appointment_packages",
                column: "patient_id",
                principalTable: "patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_patients_PatientId",
                table: "appointments",
                column: "PatientId",
                principalTable: "patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointment_packages_patients_patient_id",
                table: "appointment_packages"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_patients_PatientId",
                table: "appointments"
            );

            migrationBuilder.DropPrimaryKey(name: "PK_patients", table: "patients");

            migrationBuilder.RenameTable(name: "patients", newName: "pacients");

            migrationBuilder.AddPrimaryKey(name: "PK_pacients", table: "pacients", column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_appointment_packages_pacients_patient_id",
                table: "appointment_packages",
                column: "patient_id",
                principalTable: "pacients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_pacients_PatientId",
                table: "appointments",
                column: "PatientId",
                principalTable: "pacients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
