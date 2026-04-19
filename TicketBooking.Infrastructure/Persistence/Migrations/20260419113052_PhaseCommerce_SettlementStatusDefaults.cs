using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseCommerce_SettlementStatusDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [commerce].[CustomerSettlementBatchLines] SET [Status] = 2 WHERE [Status] = 0;");
            migrationBuilder.Sql("UPDATE [commerce].[CustomerSettlementBatches] SET [Status] = 1 WHERE [Status] = 0;");
            migrationBuilder.Sql("UPDATE [commerce].[CustomerOrders] SET [SettlementStatus] = 1 WHERE [SettlementStatus] = 0;");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "commerce",
                table: "CustomerSettlementBatchLines",
                type: "int",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "commerce",
                table: "CustomerSettlementBatches",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SettlementStatus",
                schema: "commerce",
                table: "CustomerOrders",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "commerce",
                table: "CustomerSettlementBatchLines",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 2);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "commerce",
                table: "CustomerSettlementBatches",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "SettlementStatus",
                schema: "commerce",
                table: "CustomerOrders",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);
        }
    }
}
