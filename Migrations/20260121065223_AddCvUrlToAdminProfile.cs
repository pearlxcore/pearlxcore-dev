using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearlxcore.dev.Migrations
{
    /// <inheritdoc />
    public partial class AddCvUrlToAdminProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvUrl",
                table: "AdminProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvUrl",
                table: "AdminProfiles");
        }
    }
}
