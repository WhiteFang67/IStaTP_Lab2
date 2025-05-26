using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalAPIWebApp.Migrations
{
    /// <inheritdoc />
    public partial class StatusTypesUpdate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "CarStatusTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "BookingStatusTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "CarStatusTypes");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "BookingStatusTypes");
        }
    }
}
