using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace my.money.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Quote_AssetId_AtUtc",
                table: "Quote",
                columns: new[] { "AssetId", "AtUtc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quote_AssetId_AtUtc",
                table: "Quote");
        }
    }
}
