using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampleDataAccessApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnBusRequestMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BusRequest_MessageId",
                table: "BusRequests",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusRequest_MessageId",
                table: "BusRequests");
        }
    }
}
