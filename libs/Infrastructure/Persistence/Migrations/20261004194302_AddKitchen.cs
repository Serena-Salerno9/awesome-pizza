using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FkWorkstation",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bakers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxConcurrentPizzas = table.Column<int>(type: "int", nullable: false),
                    PreparationTimePerPizza = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bakers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ovens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    BakingTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ovens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workstations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FkBaker = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FkOven = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workstations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workstations_Bakers_FkBaker",
                        column: x => x.FkBaker,
                        principalTable: "Bakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Workstations_Ovens_FkOven",
                        column: x => x.FkOven,
                        principalTable: "Ovens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FkWorkstation",
                table: "Orders",
                column: "FkWorkstation");

            migrationBuilder.CreateIndex(
                name: "IX_Workstations_FkBaker",
                table: "Workstations",
                column: "FkBaker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workstations_FkOven",
                table: "Workstations",
                column: "FkOven",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Workstations_FkWorkstation",
                table: "Orders",
                column: "FkWorkstation",
                principalTable: "Workstations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Workstations_FkWorkstation",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Workstations");

            migrationBuilder.DropTable(
                name: "Bakers");

            migrationBuilder.DropTable(
                name: "Ovens");

            migrationBuilder.DropIndex(
                name: "IX_Orders_FkWorkstation",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FkWorkstation",
                table: "Orders");
        }
    }
}
