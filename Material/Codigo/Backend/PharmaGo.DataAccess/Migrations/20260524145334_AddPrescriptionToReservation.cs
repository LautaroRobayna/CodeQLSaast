using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionToReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrescriptionBase64",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriptionFileName",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPrescription",
                table: "ReservationDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrescriptionBase64",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PrescriptionFileName",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RequiresPrescription",
                table: "ReservationDetail");
        }
    }
}
