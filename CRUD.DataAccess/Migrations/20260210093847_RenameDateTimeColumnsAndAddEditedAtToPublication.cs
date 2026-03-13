using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRUD.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameDateTimeColumnsAndAddEditedAtToPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Requests",
                newName: "Expired");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Publications",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Notifications",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "Publications",
                type: "datetime(6)",
                precision: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "Publications");

            migrationBuilder.RenameColumn(
                name: "Expired",
                table: "Requests",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Publications",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Notifications",
                newName: "Date");
        }
    }
}
