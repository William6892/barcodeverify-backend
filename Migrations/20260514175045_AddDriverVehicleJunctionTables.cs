using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BarcodeShippingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverVehicleJunctionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_TransportCompanies_TransportCompanyId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_TransportCompanies_TransportCompanyId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_TransportCompanyId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_TransportCompanyId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "TransportCompanyId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "TransportCompanyId",
                table: "Drivers");

            migrationBuilder.CreateTable(
                name: "DriverTransportCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    TransportCompanyId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverTransportCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverTransportCompanies_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverTransportCompanies_TransportCompanies_TransportCompan~",
                        column: x => x.TransportCompanyId,
                        principalTable: "TransportCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleTransportCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VehicleId = table.Column<int>(type: "integer", nullable: false),
                    TransportCompanyId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTransportCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleTransportCompanies_TransportCompanies_TransportCompa~",
                        column: x => x.TransportCompanyId,
                        principalTable: "TransportCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleTransportCompanies_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverTransportCompanies_DriverId_TransportCompanyId",
                table: "DriverTransportCompanies",
                columns: new[] { "DriverId", "TransportCompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverTransportCompanies_TransportCompanyId",
                table: "DriverTransportCompanies",
                column: "TransportCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTransportCompanies_TransportCompanyId",
                table: "VehicleTransportCompanies",
                column: "TransportCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTransportCompanies_VehicleId_TransportCompanyId",
                table: "VehicleTransportCompanies",
                columns: new[] { "VehicleId", "TransportCompanyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverTransportCompanies");

            migrationBuilder.DropTable(
                name: "VehicleTransportCompanies");

            migrationBuilder.AddColumn<int>(
                name: "TransportCompanyId",
                table: "Vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TransportCompanyId",
                table: "Drivers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TransportCompanyId",
                table: "Vehicles",
                column: "TransportCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_TransportCompanyId",
                table: "Drivers",
                column: "TransportCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_TransportCompanies_TransportCompanyId",
                table: "Drivers",
                column: "TransportCompanyId",
                principalTable: "TransportCompanies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_TransportCompanies_TransportCompanyId",
                table: "Vehicles",
                column: "TransportCompanyId",
                principalTable: "TransportCompanies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
