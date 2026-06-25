using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RoomService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "roomservice");

            migrationBuilder.CreateTable(
                name: "ServiceOrders",
                schema: "roomservice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOrderLineItems",
                schema: "roomservice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderLineItems_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalSchema: "roomservice",
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderLineItems_ServiceOrderId",
                schema: "roomservice",
                table: "ServiceOrderLineItems",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_BookingId",
                schema: "roomservice",
                table: "ServiceOrders",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_Status",
                schema: "roomservice",
                table: "ServiceOrders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceOrderLineItems",
                schema: "roomservice");

            migrationBuilder.DropTable(
                name: "ServiceOrders",
                schema: "roomservice");
        }
    }
}
