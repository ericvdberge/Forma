using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The alternate key a detail page's (WebsiteId, CollectionId) refers to. SQLite takes a unique index as the
            // parent key, and it has to exist before Pages is rebuilt with the new foreign key.
            migrationBuilder.CreateIndex(
                name: "AK_Collections_WebsiteId_Id",
                table: "Collections",
                columns: new[] { "WebsiteId", "Id" },
                unique: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "PageContents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "DraftContent",
                table: "PageContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DraftSchemaVersion",
                table: "PageContents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DraftUpdatedAt",
                table: "PageContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "PageContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DraftFields",
                table: "Collections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DraftSlug",
                table: "Collections",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DraftUpdatedAt",
                table: "Collections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DraftValueMoves",
                table: "Collections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Collections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DraftSlug",
                table: "CollectionItems",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DraftUpdatedAt",
                table: "CollectionItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DraftValues",
                table: "CollectionItems",
                type: "TEXT",
                nullable: true);

            // Existing rows: what was published is live with no draft; what wasn't becomes a draft that isn't live.
            // Collections were always live.
            migrationBuilder.Sql("""
                UPDATE "PageContents" SET "PublishedAt" = "UpdatedAt" WHERE "Status" = 'Published';
                """);

            migrationBuilder.Sql("""
                UPDATE "PageContents"
                SET "DraftContent" = "Content", "DraftSchemaVersion" = "SchemaVersion", "DraftUpdatedAt" = "UpdatedAt", "Content" = NULL
                WHERE "Status" <> 'Published';
                """);

            migrationBuilder.Sql("""
                UPDATE "Collections" SET "PublishedAt" = "CreatedAt";
                """);

            migrationBuilder.Sql("""
                UPDATE "CollectionItems" SET "PublishedAt" = COALESCE("PublishedAt", "UpdatedAt") WHERE "Status" = 'Published';
                """);

            migrationBuilder.Sql("""
                UPDATE "CollectionItems"
                SET "PublishedAt" = NULL, "DraftSlug" = "Slug", "DraftValues" = "Values", "DraftUpdatedAt" = "UpdatedAt"
                WHERE "Status" <> 'Published';
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PageContents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CollectionItems");

            migrationBuilder.CreateTable(
                name: "PageBindingUsages",
                columns: table => new
                {
                    PageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageBindingUsages", x => new { x.PageId, x.CollectionId, x.FieldId, x.Kind });
                    table.ForeignKey(
                        name: "FK_PageBindingUsages_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PageBindingUsages_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledPublishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WebsiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PublishAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Issues = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledPublishes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledPublishes_Websites_WebsiteId",
                        column: x => x.WebsiteId,
                        principalTable: "Websites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledPublishEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduledPublishId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PageId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CollectionItemId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledPublishEntries", x => x.Id);
                    table.CheckConstraint("CK_ScheduledPublishEntries_OneEntity", "(\"CollectionId\" IS NOT NULL) + (\"PageId\" IS NOT NULL) + (\"CollectionItemId\" IS NOT NULL) = 1");
                    table.ForeignKey(
                        name: "FK_ScheduledPublishEntries_CollectionItems_CollectionItemId",
                        column: x => x.CollectionItemId,
                        principalTable: "CollectionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledPublishEntries_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledPublishEntries_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledPublishEntries_ScheduledPublishes_ScheduledPublishId",
                        column: x => x.ScheduledPublishId,
                        principalTable: "ScheduledPublishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_WebsiteId_CollectionId",
                table: "Pages",
                columns: new[] { "WebsiteId", "CollectionId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pages_Kind",
                table: "Pages",
                sql: "(\"Kind\" = 'Static' AND \"CollectionId\" IS NULL) OR (\"Kind\" = 'CollectionTemplate' AND \"CollectionId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_PageBindingUsages_CollectionId_FieldId",
                table: "PageBindingUsages",
                columns: new[] { "CollectionId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPublishEntries_CollectionId",
                table: "ScheduledPublishEntries",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPublishEntries_CollectionItemId",
                table: "ScheduledPublishEntries",
                column: "CollectionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPublishEntries_PageId",
                table: "ScheduledPublishEntries",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPublishEntries_ScheduledPublishId",
                table: "ScheduledPublishEntries",
                column: "ScheduledPublishId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPublishes_Status_PublishAt",
                table: "ScheduledPublishes",
                columns: new[] { "Status", "PublishAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPublishes_WebsiteId",
                table: "ScheduledPublishes",
                column: "WebsiteId");

            migrationBuilder.DropForeignKey(
                name: "FK_Pages_Collections_CollectionId",
                table: "Pages");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_Collections_WebsiteId_CollectionId",
                table: "Pages",
                columns: new[] { "WebsiteId", "CollectionId" },
                principalTable: "Collections",
                principalColumns: new[] { "WebsiteId", "Id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pages_Collections_WebsiteId_CollectionId",
                table: "Pages");

            migrationBuilder.DropTable(
                name: "PageBindingUsages");

            migrationBuilder.DropTable(
                name: "ScheduledPublishEntries");

            migrationBuilder.DropTable(
                name: "ScheduledPublishes");

            migrationBuilder.DropIndex(
                name: "IX_Pages_WebsiteId_CollectionId",
                table: "Pages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pages_Kind",
                table: "Pages");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Collections_WebsiteId_Id",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "DraftContent",
                table: "PageContents");

            migrationBuilder.DropColumn(
                name: "DraftSchemaVersion",
                table: "PageContents");

            migrationBuilder.DropColumn(
                name: "DraftUpdatedAt",
                table: "PageContents");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "PageContents");

            migrationBuilder.DropColumn(
                name: "DraftFields",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "DraftSlug",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "DraftUpdatedAt",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "DraftValueMoves",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "DraftSlug",
                table: "CollectionItems");

            migrationBuilder.DropColumn(
                name: "DraftUpdatedAt",
                table: "CollectionItems");

            migrationBuilder.DropColumn(
                name: "DraftValues",
                table: "CollectionItems");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "PageContents",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PageContents",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CollectionItems",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_Collections_CollectionId",
                table: "Pages",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
