using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QanoonCoalition.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoPathToJoinRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "JoinRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "JoinRequests");
        }
    }
}
