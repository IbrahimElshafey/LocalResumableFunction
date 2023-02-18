#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace LocalResumableFunction.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "MethodIdentifiers",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AssemblyName = table.Column<string>("TEXT", nullable: false),
                ClassName = table.Column<string>("TEXT", nullable: false),
                MethodName = table.Column<string>("TEXT", nullable: false),
                MethodSignature = table.Column<string>("TEXT", nullable: false),
                MethodHash = table.Column<byte[]>("BLOB", maxLength: 16, nullable: false),
                Type = table.Column<int>("INTEGER", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_MethodIdentifiers", x => x.Id); });

        migrationBuilder.CreateTable(
            "FunctionStates",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                StateObject = table.Column<string>("TEXT", nullable: true),
                ResumableFunctionIdentifierId = table.Column<int>("INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FunctionStates", x => x.Id);
                table.ForeignKey(
                    "FK_FunctionsStates_For_ResumableFunction",
                    x => x.ResumableFunctionIdentifierId,
                    "MethodIdentifiers",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "Waits",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>("TEXT", nullable: false),
                Status = table.Column<int>("INTEGER", nullable: false),
                IsFirst = table.Column<bool>("INTEGER", nullable: false),
                StateBeforeWait = table.Column<int>("INTEGER", nullable: false),
                StateAfterWait = table.Column<int>("INTEGER", nullable: false),
                IsNode = table.Column<bool>("INTEGER", nullable: false),
                WaitType = table.Column<int>("INTEGER", nullable: false),
                FunctionStateId = table.Column<int>("INTEGER", nullable: false),
                RequestedByFunctionId = table.Column<int>("INTEGER", nullable: false),
                ParentWaitId = table.Column<int>("INTEGER", nullable: true),
                Discriminator = table.Column<string>("TEXT", nullable: false),
                ParentFunctionGroupId = table.Column<int>("INTEGER", nullable: true),
                FirstWaitId = table.Column<int>("INTEGER", nullable: true),
                ManyFunctionsWaitId = table.Column<int>("INTEGER", nullable: true),
                CountExpressionValue = table.Column<byte[]>("BLOB", nullable: true),
                ParentWaitsGroupId = table.Column<int>("INTEGER", nullable: true),
                IsOptional = table.Column<bool>("INTEGER", nullable: true),
                SetDataExpressionValue = table.Column<byte[]>("BLOB", nullable: true),
                MatchIfExpressionValue = table.Column<byte[]>("BLOB", nullable: true),
                NeedFunctionStateForMatch = table.Column<bool>("INTEGER", nullable: true),
                WaitMethodIdentifierId = table.Column<int>("INTEGER", nullable: true),
                ManyMethodsWaitId = table.Column<int>("INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Waits", x => x.Id);
                table.ForeignKey(
                    "FK_ChildWaits_For_Wait",
                    x => x.ParentWaitId,
                    "Waits",
                    "Id");
                table.ForeignKey(
                    "FK_FunctionsWaits_For_FunctionGroup",
                    x => x.ParentFunctionGroupId,
                    "Waits",
                    "Id");
                table.ForeignKey(
                    "FK_MethodsWaits_For_WaitsGroup",
                    x => x.ParentWaitsGroupId,
                    "Waits",
                    "Id");
                table.ForeignKey(
                    "FK_Waits_For_FunctionState",
                    x => x.FunctionStateId,
                    "FunctionStates",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_Waits_In_ResumableFunction",
                    x => x.RequestedByFunctionId,
                    "MethodIdentifiers",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_Waits_RequestedForMethod",
                    x => x.WaitMethodIdentifierId,
                    "MethodIdentifiers",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_Waits_Waits_FirstWaitId",
                    x => x.FirstWaitId,
                    "Waits",
                    "Id");
                table.ForeignKey(
                    "FK_Waits_Waits_ManyFunctionsWaitId",
                    x => x.ManyFunctionsWaitId,
                    "Waits",
                    "Id");
                table.ForeignKey(
                    "FK_Waits_Waits_ManyMethodsWaitId",
                    x => x.ManyMethodsWaitId,
                    "Waits",
                    "Id");
            });

        migrationBuilder.CreateIndex(
            "IX_FunctionStates_ResumableFunctionIdentifierId",
            "FunctionStates",
            "ResumableFunctionIdentifierId");

        migrationBuilder.CreateIndex(
            "Index_MethodHash",
            "MethodIdentifiers",
            "MethodHash",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_Waits_FirstWaitId",
            "Waits",
            "FirstWaitId");

        migrationBuilder.CreateIndex(
            "IX_Waits_FunctionStateId",
            "Waits",
            "FunctionStateId");

        migrationBuilder.CreateIndex(
            "IX_Waits_ManyFunctionsWaitId",
            "Waits",
            "ManyFunctionsWaitId");

        migrationBuilder.CreateIndex(
            "IX_Waits_ManyMethodsWaitId",
            "Waits",
            "ManyMethodsWaitId");

        migrationBuilder.CreateIndex(
            "IX_Waits_ParentFunctionGroupId",
            "Waits",
            "ParentFunctionGroupId");

        migrationBuilder.CreateIndex(
            "IX_Waits_ParentWaitId",
            "Waits",
            "ParentWaitId");

        migrationBuilder.CreateIndex(
            "IX_Waits_ParentWaitsGroupId",
            "Waits",
            "ParentWaitsGroupId");

        migrationBuilder.CreateIndex(
            "IX_Waits_RequestedByFunctionId",
            "Waits",
            "RequestedByFunctionId");

        migrationBuilder.CreateIndex(
            "IX_Waits_WaitMethodIdentifierId",
            "Waits",
            "WaitMethodIdentifierId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "Waits");

        migrationBuilder.DropTable(
            "FunctionStates");

        migrationBuilder.DropTable(
            "MethodIdentifiers");
    }
}