using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HTrack.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CorrectedTypoInAttendanceEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CkeckIn",
                table: "Attendances",
                newName: "CheckIn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CheckIn",
                table: "Attendances",
                newName: "CkeckIn");
        }
    }
}
