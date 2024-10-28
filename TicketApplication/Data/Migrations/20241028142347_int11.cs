using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketApplication.Data.Migrations
{
    /// <inheritdoc />
    public partial class int11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Zones_Events_EventId",
                table: "Zones");

            migrationBuilder.DropIndex(
                name: "IX_Zones_EventId",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Zones");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventId",
                table: "Zones",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Zones_EventId",
                table: "Zones",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Zones_Events_EventId",
                table: "Zones",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");
        }
    }
}
