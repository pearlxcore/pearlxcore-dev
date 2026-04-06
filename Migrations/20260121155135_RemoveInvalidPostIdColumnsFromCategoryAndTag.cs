using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearlxcore.dev.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvalidPostIdColumnsFromCategoryAndTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_Categories_Posts_PostId]', N'F') IS NOT NULL
    ALTER TABLE [Categories] DROP CONSTRAINT [FK_Categories_Posts_PostId];

IF OBJECT_ID(N'[FK_Tags_Posts_PostId]', N'F') IS NOT NULL
    ALTER TABLE [Tags] DROP CONSTRAINT [FK_Tags_Posts_PostId];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tags_PostId' AND object_id = OBJECT_ID(N'[Tags]'))
    DROP INDEX [IX_Tags_PostId] ON [Tags];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_PostId' AND object_id = OBJECT_ID(N'[Categories]'))
    DROP INDEX [IX_Categories_PostId] ON [Categories];

IF COL_LENGTH(N'[Tags]', N'PostId') IS NOT NULL
    ALTER TABLE [Tags] DROP COLUMN [PostId];

IF COL_LENGTH(N'[Categories]', N'PostId') IS NOT NULL
    ALTER TABLE [Categories] DROP COLUMN [PostId];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PostId",
                table: "Tags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_PostId",
                table: "Tags",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PostId",
                table: "Categories",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Posts_PostId",
                table: "Categories",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Posts_PostId",
                table: "Tags",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }
    }
}
