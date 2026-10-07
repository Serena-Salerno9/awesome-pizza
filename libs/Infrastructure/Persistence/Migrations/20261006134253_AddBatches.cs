using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FkWorkstation = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TakenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReadyAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Batches_Workstations_FkWorkstation",
                        column: x => x.FkWorkstation,
                        principalTable: "Workstations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BatchLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FkBatch = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FkOrderLine = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchLines_Batches_FkBatch",
                        column: x => x.FkBatch,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchLines_OrderLines_FkOrderLine",
                        column: x => x.FkOrderLine,
                        principalTable: "OrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Batches_FkWorkstation_ReadyAt",
                table: "Batches",
                columns: new[] { "FkWorkstation", "ReadyAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BatchLines_FkBatch",
                table: "BatchLines",
                column: "FkBatch");

            migrationBuilder.CreateIndex(
                name: "IX_BatchLines_FkOrderLine",
                table: "BatchLines",
                column: "FkOrderLine");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchLines");

            migrationBuilder.DropTable(
                name: "Batches");
        }
    }
}
