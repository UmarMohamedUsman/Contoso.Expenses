using Microsoft.EntityFrameworkCore.Migrations;

namespace Contoso.Expenses.API.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "costCenters",
                columns: table => new
                {
                    SubmitterEmail = table.Column<string>(nullable: false),
                    ApproverEmail = table.Column<string>(nullable: true),
                    CostCenterName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_costCenters", x => x.SubmitterEmail);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "costCenters");
        }
    }
}
