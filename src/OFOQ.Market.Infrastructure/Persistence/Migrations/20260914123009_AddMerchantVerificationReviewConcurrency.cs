using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantVerificationReviewConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "commerce_merchant_verification_profiles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "commerce_merchant_verification_profiles");
        }
    }
}
