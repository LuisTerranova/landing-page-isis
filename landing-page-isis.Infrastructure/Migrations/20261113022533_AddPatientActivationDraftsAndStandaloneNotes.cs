using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientActivationDraftsAndStandaloneNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_appointments_couple_id", table: "appointments");

            migrationBuilder.DropIndex(name: "IX_appointments_patient_id", table: "appointments");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "is_draft",
                table: "appointment_records",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.CreateTable(
                name: "additional_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    couple_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    note_date = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    is_deleted = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                    deleted_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_additional_notes", x => x.Id);
                    table.CheckConstraint(
                        "ck_additional_notes_exactly_one_owner",
                        "(patient_id IS NOT NULL) <> (couple_id IS NOT NULL)"
                    );
                    table.ForeignKey(
                        name: "FK_additional_notes_couples_couple_id",
                        column: x => x.couple_id,
                        principalTable: "couples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_additional_notes_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_appointments_couple_id_appointment_date",
                table: "appointments",
                columns: new[] { "couple_id", "appointment_date" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_appointments_patient_id_appointment_date",
                table: "appointments",
                columns: new[] { "patient_id", "appointment_date" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_additional_notes_couple_id_note_date",
                table: "additional_notes",
                columns: new[] { "couple_id", "note_date" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_additional_notes_patient_id_note_date",
                table: "additional_notes",
                columns: new[] { "patient_id", "note_date" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "additional_notes");

            migrationBuilder.DropIndex(
                name: "IX_appointments_couple_id_appointment_date",
                table: "appointments"
            );

            migrationBuilder.DropIndex(
                name: "IX_appointments_patient_id_appointment_date",
                table: "appointments"
            );

            migrationBuilder.DropColumn(name: "is_active", table: "patients");

            migrationBuilder.DropColumn(name: "is_draft", table: "appointment_records");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_couple_id",
                table: "appointments",
                column: "couple_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_appointments_patient_id",
                table: "appointments",
                column: "patient_id"
            );
        }
    }
}
