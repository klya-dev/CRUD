using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRUD.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsReadPropertyToUserNotificationToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "UserNotifications",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "UserNotifications");
        }
    }
}
