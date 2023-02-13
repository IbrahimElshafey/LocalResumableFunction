using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalResumableFunction.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexIndexMethodHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "Index_MethodHash",
                table: "MethodIdentifiers",
                column: "MethodHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Index_MethodHash",
                table: "MethodIdentifiers");
        }
    }
}
