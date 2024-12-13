using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketApplication.Migrations
{
    /// <inheritdoc />
    public partial class updaterefticket2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_OrderDetails_OrderDetailId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Zones_ZoneId",
                table: "Tickets");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_OrderDetails_OrderDetailId",
                table: "Tickets",
                column: "OrderDetailId",
                principalTable: "OrderDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Zones_ZoneId",
                table: "Tickets",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_OrderDetails_OrderDetailId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Zones_ZoneId",
                table: "Tickets");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_OrderDetails_OrderDetailId",
                table: "Tickets",
                column: "OrderDetailId",
                principalTable: "OrderDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Zones_ZoneId",
                table: "Tickets",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
