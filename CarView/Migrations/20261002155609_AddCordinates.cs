using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarView.Migrations
{
    /// <inheritdoc />
    public partial class AddCordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Cars",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Cars",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Cars");
        }
    }
}
