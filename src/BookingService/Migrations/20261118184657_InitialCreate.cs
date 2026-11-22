using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookingService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.PackageId);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    BookingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    CarId = table.Column<long>(type: "bigint", nullable: false),
                    WasherId = table.Column<long>(type: "bigint", nullable: true),
                    BookingDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingId);
                    table.ForeignKey(
                        name: "FK_Bookings_ServicePackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "ServicePackages",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingRejections",
                columns: table => new
                {
                    BookingRejectionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    WasherId = table.Column<long>(type: "bigint", nullable: false),
                    RejectedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRejections", x => x.BookingRejectionId);
                    table.ForeignKey(
                        name: "FK_BookingRejections_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ServicePackages",
                columns: new[] { "PackageId", "Description", "EstimatedDurationMinutes", "IsActive", "Name", "Price" },
                values: new object[,]
                {
                    { 1L, "Basic exterior wash and dry", 30, true, "Basic Wash", 299.00m },
                    { 2L, "Comprehensive interior vacuum and full exterior wash", 90, true, "Full Interior & Exterior", 599.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRejections_BookingId_WasherId",
                table: "BookingRejections",
                columns: new[] { "BookingId", "WasherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PackageId",
                table: "Bookings",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_ScheduledTime",
                table: "Bookings",
                columns: new[] { "Status", "ScheduledTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_WasherId",
                table: "Bookings",
                column: "WasherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingRejections");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "ServicePackages");
        }
    }
}
