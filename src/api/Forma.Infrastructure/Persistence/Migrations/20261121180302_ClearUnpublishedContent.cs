using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClearUnpublishedContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pages that were never published have nothing live: their document is the draft now (AddDrafts copied it).
            migrationBuilder.Sql("""
                UPDATE "PageContents" SET "Content" = NULL WHERE "PublishedAt" IS NULL AND "DraftContent" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "PageContents" SET "Content" = "DraftContent" WHERE "Content" IS NULL AND "DraftContent" IS NOT NULL;
                """);
        }
    }
}
