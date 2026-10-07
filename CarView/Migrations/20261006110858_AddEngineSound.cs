using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarView.Migrations
{
    /// <inheritdoc />
    public partial class AddEngineSound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EngineSoundUrl",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EngineSoundUrl",
                table: "Cars");
        }
    }
}
