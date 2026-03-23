using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HTrack.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceEntrySources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckInSource",
                table: "Attendances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CheckOutSource",
                table: "Attendances",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Attendances"
                SET "CheckOutSource" = 0
                WHERE "CheckOut" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInSource",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckOutSource",
                table: "Attendances");
        }
    }
}
