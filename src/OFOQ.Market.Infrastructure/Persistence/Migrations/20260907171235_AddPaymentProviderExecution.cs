using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProviderExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActionType",
                table: "commerce_payment_intents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionValue",
                table: "commerce_payment_intents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "commerce_payment_intents");

            migrationBuilder.DropColumn(
                name: "ActionValue",
                table: "commerce_payment_intents");
        }
    }
}
