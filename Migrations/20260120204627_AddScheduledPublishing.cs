using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearlxcore.dev.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledPublishAt",
                table: "Posts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledPublishAt",
                table: "Posts");
        }
    }
}
