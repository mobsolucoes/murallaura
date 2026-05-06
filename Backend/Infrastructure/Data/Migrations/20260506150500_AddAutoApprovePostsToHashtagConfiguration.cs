using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashtagWall.Infrastructure.Data.Migrations;

public partial class AddAutoApprovePostsToHashtagConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "AutoApprovePosts",
            table: "HashtagConfigurations",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AutoApprovePosts",
            table: "HashtagConfigurations");
    }
}
