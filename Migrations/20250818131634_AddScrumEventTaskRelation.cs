using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddScrumEventTaskRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScrumEventId",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ScrumEventId",
                table: "Tasks",
                column: "ScrumEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Events_ScrumEventId",
                table: "Tasks",
                column: "ScrumEventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Events_ScrumEventId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ScrumEventId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ScrumEventId",
                table: "Tasks");
        }
    }
}
