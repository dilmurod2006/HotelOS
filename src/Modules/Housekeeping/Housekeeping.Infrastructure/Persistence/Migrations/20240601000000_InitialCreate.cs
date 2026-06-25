using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Housekeeping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "housekeeping");

            migrationBuilder.CreateTable(
                name: "CleaningTasks",
                schema: "housekeeping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    AssignedCleanerId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningTasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CleaningTasks_RoomId",
                schema: "housekeeping",
                table: "CleaningTasks",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningTasks_Status",
                schema: "housekeeping",
                table: "CleaningTasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CleaningTasks", schema: "housekeeping");
        }
    }
}
