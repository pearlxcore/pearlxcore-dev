using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearlxcore.dev.Migrations
{
    /// <inheritdoc />
    public partial class AddRenderedContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RenderedContent",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RenderedContent",
                table: "Posts");
        }
    }
}
