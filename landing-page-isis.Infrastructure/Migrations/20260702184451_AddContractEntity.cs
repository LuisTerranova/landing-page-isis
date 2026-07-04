using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddContractEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    patient_cpf = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    patient_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    patient_phone = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    patient_state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    patient_birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    terms_accepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: true),
                    initial_appointments = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<string>(type: "text", nullable: true),
                    professional_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    contract_document_html = table.Column<string>(type: "text", nullable: true),
                    package_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contracts_appointment_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "appointment_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contracts_package_id",
                table: "contracts",
                column: "package_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contracts");
        }
    }
}
