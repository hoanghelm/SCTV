using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Context.Migrations.Identity
{
    /// <inheritdoc />
    public partial class AddDefaultUserStatus : Migration
    {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.InsertData(
				table: "UserStatus",
				columns: new[] { "Id", "Name", "Description" },
				values: new object[,]
				{
			{ "active", "Active", "User account is active and can access the system" },
			{ "inactive", "Inactive", "User account is inactive" },
			{ "suspended", "Suspended", "User account has been temporarily suspended" },
			{ "deleted", "Deleted", "User account has been deleted" }
				});
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DeleteData(
				table: "UserStatus",
				keyColumn: "Id",
				keyValues: new object[] { "active", "inactive", "suspended", "deleted" });
		}
	}
}
