using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeRefine.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GitHubLogin = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GitHubRepoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DefaultBranch = table.Column<string>(type: "text", nullable: false),
                    InstallationId = table.Column<long>(type: "bigint", nullable: true),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Repositories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PullRequestNumber = table.Column<int>(type: "integer", nullable: true),
                    SourceBranch = table.Column<string>(type: "text", nullable: true),
                    TargetBranch = table.Column<string>(type: "text", nullable: true),
                    CommitSha = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    QualityScoreBefore = table.Column<double>(type: "double precision", nullable: true),
                    QualityScoreAfter = table.Column<double>(type: "double precision", nullable: true),
                    ImprovementBranch = table.Column<string>(type: "text", nullable: true),
                    ImprovementPrNumber = table.Column<int>(type: "integer", nullable: true),
                    ImprovementPrUrl = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisRuns_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StartLine = table.Column<int>(type: "integer", nullable: false),
                    EndLine = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Recommendation = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Findings_AnalysisRuns_AnalysisRunId",
                        column: x => x.AnalysisRunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Output = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationResults_AnalysisRuns_AnalysisRunId",
                        column: x => x.AnalysisRunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OriginalCode = table.Column<string>(type: "text", nullable: false),
                    ProposedCode = table.Column<string>(type: "text", nullable: false),
                    Diff = table.Column<string>(type: "text", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patches_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_AnalysisRuns_AnalysisRunId",
                        column: x => x.AnalysisRunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Patches_PatchId",
                        column: x => x.PatchId,
                        principalTable: "Patches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_RepositoryId_StartedAt",
                table: "AnalysisRuns",
                columns: new[] { "RepositoryId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_AnalysisRunId",
                table: "Findings",
                column: "AnalysisRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Patches_FindingId",
                table: "Patches",
                column: "FindingId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Owner_Name",
                table: "Repositories",
                columns: new[] { "Owner", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_UserId",
                table: "Repositories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AnalysisRunId",
                table: "Reviews",
                column: "AnalysisRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_FindingId",
                table: "Reviews",
                column: "FindingId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_PatchId",
                table: "Reviews",
                column: "PatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GitHubLogin",
                table: "Users",
                column: "GitHubLogin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationResults_AnalysisRunId_CheckType",
                table: "VerificationResults",
                columns: new[] { "AnalysisRunId", "CheckType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "VerificationResults");

            migrationBuilder.DropTable(
                name: "Patches");

            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "AnalysisRuns");

            migrationBuilder.DropTable(
                name: "Repositories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
