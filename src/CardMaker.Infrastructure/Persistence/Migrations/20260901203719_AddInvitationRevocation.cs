using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardMaker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationRevocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "Invitations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "Invitations");
        }
    }
}
