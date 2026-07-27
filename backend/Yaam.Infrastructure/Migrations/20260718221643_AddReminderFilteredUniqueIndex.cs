using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yaam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderFilteredUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reminders_ApplicationId",
                table: "Reminders");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_ApplicationId",
                table: "Reminders",
                column: "ApplicationId",
                unique: true,
                filter: "\"CompletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reminders_ApplicationId",
                table: "Reminders");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_ApplicationId",
                table: "Reminders",
                column: "ApplicationId",
                unique: true);
        }
    }
}
