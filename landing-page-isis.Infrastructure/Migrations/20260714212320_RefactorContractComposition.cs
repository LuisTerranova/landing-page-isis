using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class RefactorContractComposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_contracts_patient2_cpf_hash",
                table: "contracts",
                column: "patient2_cpf_hash",
                unique: true,
                filter: "\"patient2_cpf_hash\" IS NOT NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_contracts_patient2_cpf_hash", table: "contracts");
        }
    }
}
