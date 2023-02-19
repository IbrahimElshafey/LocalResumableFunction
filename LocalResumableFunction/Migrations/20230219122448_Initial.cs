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
                name: "MethodIdentifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssemblyName = table.Column<string>(type: "TEXT", nullable: true),
                    ClassName = table.Column<string>(type: "TEXT", nullable: true),
                    MethodName = table.Column<string>(type: "TEXT", nullable: true),
                    MethodSignature = table.Column<string>(type: "TEXT", nullable: true),
                    MethodHash = table.Column<byte[]>(type: "BLOB", maxLength: 16, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodIdentifiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FunctionStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    StateObject = table.Column<string>(type: "TEXT", nullable: true),
                    ResumableFunctionIdentifierId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FunctionsStates_For_ResumableFunction",
                        column: x => x.ResumableFunctionIdentifierId,
                        principalTable: "MethodIdentifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Waits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFirst = table.Column<bool>(type: "INTEGER", nullable: false),
                    StateBeforeWait = table.Column<int>(type: "INTEGER", nullable: false),
                    StateAfterWait = table.Column<int>(type: "INTEGER", nullable: false),
                    IsNode = table.Column<bool>(type: "INTEGER", nullable: false),
                    WaitType = table.Column<int>(type: "INTEGER", nullable: false),
                    FunctionStateId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedByFunctionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentWaitId = table.Column<int>(type: "INTEGER", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    CountExpressionValue = table.Column<byte[]>(type: "BLOB", nullable: true),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: true),
                    SetDataExpressionValue = table.Column<byte[]>(type: "BLOB", nullable: true),
                    MatchIfExpressionValue = table.Column<byte[]>(type: "BLOB", nullable: true),
                    NeedFunctionStateForMatch = table.Column<bool>(type: "INTEGER", nullable: true),
                    WaitMethodIdentifierId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildWaits_For_Wait",
                        column: x => x.ParentWaitId,
                        principalTable: "Waits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Waits_For_FunctionState",
                        column: x => x.FunctionStateId,
                        principalTable: "FunctionStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Waits_In_ResumableFunction",
                        column: x => x.RequestedByFunctionId,
                        principalTable: "MethodIdentifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Waits_RequestedForMethod",
                        column: x => x.WaitMethodIdentifierId,
                        principalTable: "MethodIdentifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionStates_ResumableFunctionIdentifierId",
                table: "FunctionStates",
                column: "ResumableFunctionIdentifierId");

            migrationBuilder.CreateIndex(
                name: "Index_MethodHash",
                table: "MethodIdentifiers",
                column: "MethodHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Waits_FunctionStateId",
                table: "Waits",
                column: "FunctionStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_ParentWaitId",
                table: "Waits",
                column: "ParentWaitId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_RequestedByFunctionId",
                table: "Waits",
                column: "RequestedByFunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_WaitMethodIdentifierId",
                table: "Waits",
                column: "WaitMethodIdentifierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Waits");

            migrationBuilder.DropTable(
                name: "FunctionStates");

            migrationBuilder.DropTable(
                name: "MethodIdentifiers");
        }
    }
}
