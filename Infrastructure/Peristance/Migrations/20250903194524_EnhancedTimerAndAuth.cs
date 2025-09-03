using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portal.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedTimerAndAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MagicLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LeagueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ParticipantName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UsedByIpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EventData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersistedTimers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerieAPlayerId = table.Column<int>(type: "int", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InitialSeconds = table.Column<int>(type: "int", nullable: false),
                    WarningAtSeconds = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPaused = table.Column<bool>(type: "bit", nullable: false),
                    PausedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PausedTotalSeconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistedTimers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    TeamName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SerieAPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    RoleExtended = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Team = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    QuotationA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuotationI = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuotationAMantra = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    QuotationIMantra = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FVM = table.Column<int>(type: "int", nullable: false),
                    FVMMantra = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerieAPlayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuctionSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentRole = table.Column<int>(type: "int", nullable: false),
                    CurrentOrderIndex = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<int>(type: "int", nullable: false),
                    MinIncrement = table.Column<int>(type: "int", nullable: false),
                    IsBiddingActive = table.Column<bool>(type: "bit", nullable: false),
                    CurrentHighestBid = table.Column<int>(type: "int", nullable: false),
                    CurrentHighestTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentSerieAPlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionSessions_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Budget = table.Column<int>(type: "int", nullable: false),
                    MaxP = table.Column<int>(type: "int", nullable: false),
                    MaxD = table.Column<int>(type: "int", nullable: false),
                    MaxC = table.Column<int>(type: "int", nullable: false),
                    MaxA = table.Column<int>(type: "int", nullable: false),
                    CountP = table.Column<int>(type: "int", nullable: false),
                    CountD = table.Column<int>(type: "int", nullable: false),
                    CountC = table.Column<int>(type: "int", nullable: false),
                    CountA = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuctionTurns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsTimerActive = table.Column<bool>(type: "bit", nullable: false),
                    TimerStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemainingSeconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionTurns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionTurns_AuctionSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AuctionSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuctionTurns_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuctionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionParticipants_AuctionSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AuctionSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuctionParticipants_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerOwnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaguePlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerieAPlayerId = table.Column<int>(type: "int", nullable: false),
                    PurchasePrice = table.Column<int>(type: "int", nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuctionEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerOwnerships_SerieAPlayers_SerieAPlayerId",
                        column: x => x.SerieAPlayerId,
                        principalTable: "SerieAPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerOwnerships_Teams_LeaguePlayerId",
                        column: x => x.LeaguePlayerId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    PlacedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bids_AuctionTurns_TurnId",
                        column: x => x.TurnId,
                        principalTable: "AuctionTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bids_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionParticipants_SessionId_OrderIndex",
                table: "AuctionParticipants",
                columns: new[] { "SessionId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuctionParticipants_TeamId",
                table: "AuctionParticipants",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionSessions_LeagueId",
                table: "AuctionSessions",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionTurns_PlayerId",
                table: "AuctionTurns",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionTurns_SessionId",
                table: "AuctionTurns",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_TeamId",
                table: "Bids",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_TurnId",
                table: "Bids",
                column: "TurnId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicLinks_ExpiresAt",
                table: "MagicLinks",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MagicLinks_IsActive_IsUsed",
                table: "MagicLinks",
                columns: new[] { "IsActive", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_MagicLinks_LeagueId",
                table: "MagicLinks",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicLinks_TeamId",
                table: "MagicLinks",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicLinks_Token",
                table: "MagicLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_CreatedAt",
                table: "OutboxEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_NextRetryAt",
                table: "OutboxEvents",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Processed",
                table: "OutboxEvents",
                column: "Processed");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Processed_NextRetryAt",
                table: "OutboxEvents",
                columns: new[] { "Processed", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PersistedTimers_AuctionId",
                table: "PersistedTimers",
                column: "AuctionId");

            migrationBuilder.CreateIndex(
                name: "IX_PersistedTimers_IsActive_ExpiresAt",
                table: "PersistedTimers",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PersistedTimers_IsActive_IsPaused",
                table: "PersistedTimers",
                columns: new[] { "IsActive", "IsPaused" });

            migrationBuilder.CreateIndex(
                name: "IX_PersistedTimers_TurnId",
                table: "PersistedTimers",
                column: "TurnId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ownership_UniqueActive",
                table: "PlayerOwnerships",
                columns: new[] { "LeaguePlayerId", "SerieAPlayerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerOwnerships_SerieAPlayerId",
                table: "PlayerOwnerships",
                column: "SerieAPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LeagueId_Name",
                table: "Teams",
                columns: new[] { "LeagueId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionParticipants");

            migrationBuilder.DropTable(
                name: "Bids");

            migrationBuilder.DropTable(
                name: "MagicLinks");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropTable(
                name: "PersistedTimers");

            migrationBuilder.DropTable(
                name: "PlayerOwnerships");

            migrationBuilder.DropTable(
                name: "AuctionTurns");

            migrationBuilder.DropTable(
                name: "SerieAPlayers");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "AuctionSessions");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Leagues");
        }
    }
}
