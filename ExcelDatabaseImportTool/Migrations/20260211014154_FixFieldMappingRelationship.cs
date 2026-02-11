using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelDatabaseImportTool.Migrations
{
    /// <inheritdoc />
    public partial class FixFieldMappingRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatabaseConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Server = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Database = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionString = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForeignKeyMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReferencedTable = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ReferencedLookupField = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ReferencedKeyField = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForeignKeyMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DatabaseConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HasHeaderRow = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportConfigurations_DatabaseConfigurations_DatabaseConfigurationId",
                        column: x => x.DatabaseConfigurationId,
                        principalTable: "DatabaseConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExcelColumnName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DatabaseFieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ImportConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ForeignKeyMappingId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldMappings_ForeignKeyMappings_ForeignKeyMappingId",
                        column: x => x.ForeignKeyMappingId,
                        principalTable: "ForeignKeyMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FieldMappings_ImportConfigurations_ImportConfigurationId",
                        column: x => x.ImportConfigurationId,
                        principalTable: "ImportConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExcelFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalRecords = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulRecords = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedRecords = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorDetails = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportLogs_ImportConfigurations_ImportConfigurationId",
                        column: x => x.ImportConfigurationId,
                        principalTable: "ImportConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_ForeignKeyMappingId",
                table: "FieldMappings",
                column: "ForeignKeyMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_ImportConfigurationId",
                table: "FieldMappings",
                column: "ImportConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportConfigurations_DatabaseConfigurationId",
                table: "ImportConfigurations",
                column: "DatabaseConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_ImportConfigurationId",
                table: "ImportLogs",
                column: "ImportConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldMappings");

            migrationBuilder.DropTable(
                name: "ImportLogs");

            migrationBuilder.DropTable(
                name: "ForeignKeyMappings");

            migrationBuilder.DropTable(
                name: "ImportConfigurations");

            migrationBuilder.DropTable(
                name: "DatabaseConfigurations");
        }
    }
}
