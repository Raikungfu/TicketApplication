using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketApplication.Migrations
{
    /// <inheritdoc />
    public partial class revert : Migration
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

            migrationBuilder.AlterColumn<string>(
                name: "ZoneId",
                table: "Tickets",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrderDetailId",
                table: "Tickets",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_OrderDetails_OrderDetailId",
                table: "Tickets",
                column: "OrderDetailId",
                principalTable: "OrderDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Zones_ZoneId",
                table: "Tickets",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.AlterColumn<string>(
                name: "ZoneId",
                table: "Tickets",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "OrderDetailId",
                table: "Tickets",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

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
    }
}
