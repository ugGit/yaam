using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yaam.Reminders.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderApplicationForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_ApplicationSnapshots_ApplicationId",
                table: "Reminders",
                column: "ApplicationId",
                principalTable: "ApplicationSnapshots",
                principalColumn: "ApplicationId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_ApplicationSnapshots_ApplicationId",
                table: "Reminders");
        }
    }
}
