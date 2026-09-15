using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRUD.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueFlagToTokenProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ConfirmEmailRequests_Token",
                table: "ConfirmEmailRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangePasswordRequests_Token",
                table: "ChangePasswordRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthRefreshTokens_Token",
                table: "AuthRefreshTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConfirmEmailRequests_Token",
                table: "ConfirmEmailRequests");

            migrationBuilder.DropIndex(
                name: "IX_ChangePasswordRequests_Token",
                table: "ChangePasswordRequests");

            migrationBuilder.DropIndex(
                name: "IX_AuthRefreshTokens_Token",
                table: "AuthRefreshTokens");
        }
    }
}
