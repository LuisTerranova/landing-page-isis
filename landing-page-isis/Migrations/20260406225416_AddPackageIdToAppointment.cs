using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageIdToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "package_id",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_package_id",
                table: "appointments",
                column: "package_id");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_appointment_packages_package_id",
                table: "appointments",
                column: "package_id",
                principalTable: "appointment_packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_appointment_packages_package_id",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_package_id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "package_id",
                table: "appointments");
        }
    }
}
