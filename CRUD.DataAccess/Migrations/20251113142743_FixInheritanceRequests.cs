using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRUD.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixInheritanceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_UserId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_UserId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Requests");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "VerificationPhoneNumberRequests",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ConfirmEmailRequests",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ChangePasswordRequests",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationPhoneNumberRequests_UserId",
                table: "VerificationPhoneNumberRequests",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmEmailRequests_UserId",
                table: "ConfirmEmailRequests",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangePasswordRequests_UserId",
                table: "ChangePasswordRequests",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangePasswordRequests_Users_UserId",
                table: "ChangePasswordRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfirmEmailRequests_Users_UserId",
                table: "ConfirmEmailRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VerificationPhoneNumberRequests_Users_UserId",
                table: "VerificationPhoneNumberRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChangePasswordRequests_Users_UserId",
                table: "ChangePasswordRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfirmEmailRequests_Users_UserId",
                table: "ConfirmEmailRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_VerificationPhoneNumberRequests_Users_UserId",
                table: "VerificationPhoneNumberRequests");

            migrationBuilder.DropIndex(
                name: "IX_VerificationPhoneNumberRequests_UserId",
                table: "VerificationPhoneNumberRequests");

            migrationBuilder.DropIndex(
                name: "IX_ConfirmEmailRequests_UserId",
                table: "ConfirmEmailRequests");

            migrationBuilder.DropIndex(
                name: "IX_ChangePasswordRequests_UserId",
                table: "ChangePasswordRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "VerificationPhoneNumberRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ConfirmEmailRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChangePasswordRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Requests",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_UserId",
                table: "Requests",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_UserId",
                table: "Requests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
