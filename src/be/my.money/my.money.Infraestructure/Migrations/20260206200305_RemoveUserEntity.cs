using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace my.money.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Holding_Portfolios_PortfolioId",
                table: "Holding");

            migrationBuilder.DropForeignKey(
                name: "FK_Quote_Assets_AssetId",
                table: "Quote");

            migrationBuilder.DropForeignKey(
                name: "FK_Trade_Portfolios_PortfolioId",
                table: "Trade");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trade",
                table: "Trade");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Quote",
                table: "Quote");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Holding",
                table: "Holding");

            migrationBuilder.RenameTable(
                name: "Trade",
                newName: "Trades");

            migrationBuilder.RenameTable(
                name: "Quote",
                newName: "Quotes");

            migrationBuilder.RenameTable(
                name: "Holding",
                newName: "Holdings");

            migrationBuilder.RenameIndex(
                name: "IX_Trade_PortfolioId",
                table: "Trades",
                newName: "IX_Trades_PortfolioId");

            migrationBuilder.RenameIndex(
                name: "IX_Quote_AssetId",
                table: "Quotes",
                newName: "IX_Quotes_AssetId");

            migrationBuilder.RenameIndex(
                name: "IX_Holding_PortfolioId",
                table: "Holdings",
                newName: "IX_Holdings_PortfolioId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trades",
                table: "Trades",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Quotes",
                table: "Quotes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Holdings",
                table: "Holdings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Holdings_Portfolios_PortfolioId",
                table: "Holdings",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Assets_AssetId",
                table: "Quotes",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Portfolios_PortfolioId",
                table: "Trades",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Holdings_Portfolios_PortfolioId",
                table: "Holdings");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Assets_AssetId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Portfolios_PortfolioId",
                table: "Trades");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trades",
                table: "Trades");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Quotes",
                table: "Quotes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Holdings",
                table: "Holdings");

            migrationBuilder.RenameTable(
                name: "Trades",
                newName: "Trade");

            migrationBuilder.RenameTable(
                name: "Quotes",
                newName: "Quote");

            migrationBuilder.RenameTable(
                name: "Holdings",
                newName: "Holding");

            migrationBuilder.RenameIndex(
                name: "IX_Trades_PortfolioId",
                table: "Trade",
                newName: "IX_Trade_PortfolioId");

            migrationBuilder.RenameIndex(
                name: "IX_Quotes_AssetId",
                table: "Quote",
                newName: "IX_Quote_AssetId");

            migrationBuilder.RenameIndex(
                name: "IX_Holdings_PortfolioId",
                table: "Holding",
                newName: "IX_Holding_PortfolioId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trade",
                table: "Trade",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Quote",
                table: "Quote",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Holding",
                table: "Holding",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Holding_Portfolios_PortfolioId",
                table: "Holding",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quote_Assets_AssetId",
                table: "Quote",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trade_Portfolios_PortfolioId",
                table: "Trade",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
