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
                    AssemblyName = table.Column<string>(type: "TEXT", nullable: false),
                    ClassName = table.Column<string>(type: "TEXT", nullable: false),
                    MethodName = table.Column<string>(type: "TEXT", nullable: false),
                    MethodSignature = table.Column<string>(type: "TEXT", nullable: false),
                    MethodHash = table.Column<byte[]>(type: "BLOB", maxLength: 16, nullable: false),
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
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFirst = table.Column<bool>(type: "INTEGER", nullable: false),
                    StateAfterWait = table.Column<int>(type: "INTEGER", nullable: false),
                    IsNode = table.Column<bool>(type: "INTEGER", nullable: false),
                    WaitType = table.Column<int>(type: "INTEGER", nullable: false),
                    FunctionStateId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedByFunctionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentWaitId = table.Column<int>(type: "INTEGER", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    ParentFunctionGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    FirstWaitId = table.Column<int>(type: "INTEGER", nullable: true),
                    ManyFunctionsWaitId = table.Column<int>(type: "INTEGER", nullable: true),
                    WhenCountExpression = table.Column<string>(type: "TEXT", nullable: true),
                    ParentWaitsGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: true),
                    NeedFunctionStateForMatch = table.Column<bool>(type: "INTEGER", nullable: true),
                    WaitMethodIdentifierId = table.Column<int>(type: "INTEGER", nullable: true),
                    ManyMethodsWaitId = table.Column<int>(type: "INTEGER", nullable: true)
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
                        name: "FK_FunctionsWaits_For_FunctionGroup",
                        column: x => x.ParentFunctionGroupId,
                        principalTable: "Waits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MethodsWaits_For_WaitsGroup",
                        column: x => x.ParentWaitsGroupId,
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
                    table.ForeignKey(
                        name: "FK_Waits_Waits_FirstWaitId",
                        column: x => x.FirstWaitId,
                        principalTable: "Waits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Waits_Waits_ManyFunctionsWaitId",
                        column: x => x.ManyFunctionsWaitId,
                        principalTable: "Waits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Waits_Waits_ManyMethodsWaitId",
                        column: x => x.ManyMethodsWaitId,
                        principalTable: "Waits",
                        principalColumn: "Id");
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
                name: "IX_Waits_FirstWaitId",
                table: "Waits",
                column: "FirstWaitId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_FunctionStateId",
                table: "Waits",
                column: "FunctionStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_ManyFunctionsWaitId",
                table: "Waits",
                column: "ManyFunctionsWaitId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_ManyMethodsWaitId",
                table: "Waits",
                column: "ManyMethodsWaitId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_ParentFunctionGroupId",
                table: "Waits",
                column: "ParentFunctionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_ParentWaitId",
                table: "Waits",
                column: "ParentWaitId");

            migrationBuilder.CreateIndex(
                name: "IX_Waits_ParentWaitsGroupId",
                table: "Waits",
                column: "ParentWaitsGroupId");

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
