using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampleDataAccessApi.Migrations
{
    /// <inheritdoc />
    public partial class MovieIdFKBusRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MovieId",
                table: "BusRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusRequest_MovieId",
                table: "BusRequests",
                column: "MovieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusRequest_MovieId",
                table: "BusRequests");

            migrationBuilder.DropColumn(
                name: "MovieId",
                table: "BusRequests");
        }
    }
}
