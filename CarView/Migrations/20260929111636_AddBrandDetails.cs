using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarView.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnnualSales",
                table: "Brands",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Founders",
                table: "Brands",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Brands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PopularModels",
                table: "Brands",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "YearFounded",
                table: "Brands",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualSales",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Founders",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "PopularModels",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "YearFounded",
                table: "Brands");
        }
    }
}
