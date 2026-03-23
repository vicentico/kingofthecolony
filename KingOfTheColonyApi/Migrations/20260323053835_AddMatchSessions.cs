using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingOfTheColonyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameRoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    KingUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChallengerUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    WinnerUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    LoserUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    GameRom = table.Column<string>(type: "TEXT", nullable: false),
                    ResultSource = table.Column<string>(type: "TEXT", nullable: false),
                    LaunchSource = table.Column<string>(type: "TEXT", nullable: false),
                    ClientInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    EvidencePayload = table.Column<string>(type: "TEXT", nullable: true),
                    ResultReason = table.Column<string>(type: "TEXT", nullable: true),
                    LastIdempotencyKey = table.Column<string>(type: "TEXT", nullable: true),
                    EmulatorProcessId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReportedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchSessions_GameRooms_GameRoomId",
                        column: x => x.GameRoomId,
                        principalTable: "GameRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchSessions_Users_ChallengerUserId",
                        column: x => x.ChallengerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchSessions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchSessions_Users_KingUserId",
                        column: x => x.KingUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchSessions_Users_LoserUserId",
                        column: x => x.LoserUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchSessions_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchSessions_Users_WinnerUserId",
                        column: x => x.WinnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchSessions_ChallengerUserId",
                table: "MatchSessions",
                column: "ChallengerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSessions_CreatedByUserId",
                table: "MatchSessions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSessions_GameRoomId_Status",
                table: "MatchSessions",
                columns: new[] { "GameRoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchSessions_KingUserId",
                table: "MatchSessions",
                column: "KingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSessions_LoserUserId",
                table: "MatchSessions",
                column: "LoserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSessions_ReportedByUserId",
                table: "MatchSessions",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSessions_WinnerUserId",
                table: "MatchSessions",
                column: "WinnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchSessions");
        }
    }
}
