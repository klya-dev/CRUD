using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRUD.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddInheritanceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "CreatedAt",
                table: "VerificationPhoneNumberRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VerificationPhoneNumberRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "VerificationPhoneNumberRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ConfirmEmailRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ConfirmEmailRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ConfirmEmailRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ChangePasswordRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ChangePasswordRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChangePasswordRequests");

            migrationBuilder.CreateTable(
                name: "Requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_UserId",
                table: "Requests",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangePasswordRequests_Requests_Id",
                table: "ChangePasswordRequests",
                column: "Id",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfirmEmailRequests_Requests_Id",
                table: "ConfirmEmailRequests",
                column: "Id",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VerificationPhoneNumberRequests_Requests_Id",
                table: "VerificationPhoneNumberRequests",
                column: "Id",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChangePasswordRequests_Requests_Id",
                table: "ChangePasswordRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfirmEmailRequests_Requests_Id",
                table: "ConfirmEmailRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_VerificationPhoneNumberRequests_Requests_Id",
                table: "VerificationPhoneNumberRequests");

            migrationBuilder.DropTable(
                name: "Requests");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "VerificationPhoneNumberRequests",
                type: "datetime(6)",
                precision: 6,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "VerificationPhoneNumberRequests",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "VerificationPhoneNumberRequests",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ConfirmEmailRequests",
                type: "datetime(6)",
                precision: 6,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "ConfirmEmailRequests",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ConfirmEmailRequests",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ChangePasswordRequests",
                type: "datetime(6)",
                precision: 6,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "ChangePasswordRequests",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true);

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
    }
}
