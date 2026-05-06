using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashtagWall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HashtagConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedHashtag = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InstagramBusinessAccountId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MetaAccessToken = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    PollIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsMonitoringEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashtagConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstagramMediaPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HashtagConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstagramMediaId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Hashtag = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Caption = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    MediaUrl = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Permalink = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstagramMediaPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstagramMediaPosts_HashtagConfigurations_HashtagConfigurat~",
                        column: x => x.HashtagConfigurationId,
                        principalTable: "HashtagConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HashtagConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Details = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationLogs_HashtagConfigurations_HashtagConfigurationId",
                        column: x => x.HashtagConfigurationId,
                        principalTable: "HashtagConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WallConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HashtagConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PrimaryColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Theme = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ShowCaption = table.Column<bool>(type: "boolean", nullable: false),
                    ShowQrCode = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WallConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WallConfigurations_HashtagConfigurations_HashtagConfigurati~",
                        column: x => x.HashtagConfigurationId,
                        principalTable: "HashtagConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Username",
                table: "AdminUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HashtagConfigurations_NormalizedHashtag",
                table: "HashtagConfigurations",
                column: "NormalizedHashtag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstagramMediaPosts_HashtagConfigurationId_InstagramMediaId",
                table: "InstagramMediaPosts",
                columns: new[] { "HashtagConfigurationId", "InstagramMediaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_HashtagConfigurationId",
                table: "IntegrationLogs",
                column: "HashtagConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_WallConfigurations_HashtagConfigurationId",
                table: "WallConfigurations",
                column: "HashtagConfigurationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "InstagramMediaPosts");

            migrationBuilder.DropTable(
                name: "IntegrationLogs");

            migrationBuilder.DropTable(
                name: "WallConfigurations");

            migrationBuilder.DropTable(
                name: "HashtagConfigurations");
        }
    }
}
