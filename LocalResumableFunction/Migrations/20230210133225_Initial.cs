using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalResumableFunction.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FunctionRuntimeInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InitiatedByClassType = table.Column<string>(type: "TEXT", nullable: false),
                    FunctionState = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionRuntimeInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MethodIdentifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssemblyName = table.Column<string>(type: "TEXT", nullable: false),
                    ClassName = table.Column<string>(type: "TEXT", nullable: false),
                    MethodName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodIdentifiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Waits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFirst = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSingle = table.Column<bool>(type: "INTEGER", nullable: false),
                    StateAfterWait = table.Column<int>(type: "INTEGER", nullable: false),
                    IsNode = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReplayType = table.Column<int>(type: "INTEGER", nullable: true),
                    FunctionRuntimeInfoId = table.Column<int>(type: "INTEGER", nullable: false),
                    MethodIdentifierId = table.Column<int>(type: "INTEGER", nullable: false),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    WhenCountExpression = table.Column<string>(type: "TEXT", nullable: true),
                    ParentWaitsGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: true),
                    SetDataExpression = table.Column<string>(type: "TEXT", nullable: true),
                    MatchIfExpression = table.Column<string>(type: "TEXT", nullable: true),
                    NeedFunctionDataForMatch = table.Column<bool>(type: "INTEGER", nullable: true),
                    ManyMethodsWaitId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WMethodsWaits_For_WaitsGroup",
                        column: x => x.ParentWaitsGroupId,
                        principalTable: "Waits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Waits_For_FunctionRuntimeInfo",
                        column: x => x.FunctionRuntimeInfoId,
                        principalTable: "FunctionRuntimeInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Waits_For_MethodIdentifier",
                        column: x => x.MethodIdentifierId,
                        principalTable: "MethodIdentifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Waits_Waits_ManyMethodsWaitId",
                        column: x => x.ManyMethodsWaitId,
                        principalTable: "Waits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Waits_FunctionRuntimeInfoId",
                table: "Waits",
                column: "FunctionRuntimeInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_ManyMethodsWaitId",
                table: "Waits",
                column: "ManyMethodsWaitId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_MethodIdentifierId",
                table: "Waits",
                column: "MethodIdentifierId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_ParentWaitsGroupId",
                table: "Waits",
                column: "ParentWaitsGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Waits");

            migrationBuilder.DropTable(
                name: "FunctionRuntimeInfos");

            migrationBuilder.DropTable(
                name: "MethodIdentifiers");
        }
    }
}
