using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonDetections.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.CreateTable(
                name: "PersonDetections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CameraId = table.Column<Guid>(type: "uuid", nullable: false),
                    CameraName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DetectionCount = table.Column<int>(type: "integer", nullable: false),
                    DetectionsData = table.Column<string>(type: "jsonb", nullable: false),
                    FrameData = table.Column<string>(type: "text", nullable: true),
                    FrameStoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonDetections", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonDetections");
        }
    }
}
