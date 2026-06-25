using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Reception.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reception");

            migrationBuilder.CreateTable(
                name: "Guests",
                schema: "reception",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaskedCardLast4 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                schema: "reception",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomNumber_Floor = table.Column<int>(type: "integer", nullable: false),
                    RoomNumber_Unit = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NightlyRate_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NightlyRate_Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: false),
                    IsNearElevator = table.Column<bool>(type: "boolean", nullable: false),
                    IsNearStaircase = table.Column<bool>(type: "boolean", nullable: false),
                    LastCleanedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "reception",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckIn = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOut = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedRoomType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreferredFloor = table.Column<int>(type: "integer", nullable: true),
                    ProximityPreference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoomCharge_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RoomCharge_Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    RoomServiceCharges_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RoomServiceCharges_Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExtraFees_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExtraFees_Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TotalBill_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalBill_Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Guests_GuestId",
                        column: x => x.GuestId,
                        principalSchema: "reception",
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "reception",
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingExtraFees",
                schema: "reception",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount_Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Amount_Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingExtraFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingExtraFees_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "reception",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingExtraFees_BookingId",
                schema: "reception",
                table: "BookingExtraFees",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_GuestId",
                schema: "reception",
                table: "Bookings",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomId_Status",
                schema: "reception",
                table: "Bookings",
                columns: new[] { "RoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_Guests_Email",
                schema: "reception",
                table: "Guests",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_LastCleanedAt",
                schema: "reception",
                table: "Rooms",
                column: "LastCleanedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Type_Status",
                schema: "reception",
                table: "Rooms",
                columns: new[] { "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_Rooms_RoomNumber",
                schema: "reception",
                table: "Rooms",
                columns: new[] { "RoomNumber_Floor", "RoomNumber_Unit" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingExtraFees",
                schema: "reception");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "reception");

            migrationBuilder.DropTable(
                name: "Guests",
                schema: "reception");

            migrationBuilder.DropTable(
                name: "Rooms",
                schema: "reception");
        }
    }
}
