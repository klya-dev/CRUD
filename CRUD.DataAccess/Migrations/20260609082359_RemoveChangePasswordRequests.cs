using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRUD.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChangePasswordRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangePasswordRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangePasswordRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    HashedNewPassword = table.Column<string>(type: "varchar(69)", maxLength: 69, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Token = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangePasswordRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangePasswordRequests_Requests_Id",
                        column: x => x.Id,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangePasswordRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ChangePasswordRequests_Token",
                table: "ChangePasswordRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangePasswordRequests_UserId",
                table: "ChangePasswordRequests",
                column: "UserId",
                unique: true);
        }
    }
}
