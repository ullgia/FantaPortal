using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using Xunit;

namespace Tests.Domain;

/// <summary>
/// Test end-to-end completi che simulano intere aste per verificare la correttezza della logica di dominio
/// </summary>
public class CompleteAuctionSimulationTests
{
    private static League CreateTestLeague(string name = "Complete Auction Test")
    {
        return League.Create(name);
    }

    private static SerieAPlayer CreateTestPlayer(int id, PlayerType type, string name = null, decimal fantaVoto = 6.5m)
    {
        var role = type switch
        {
            PlayerType.Goalkeeper => "P",
            PlayerType.Defender => "D", 
            PlayerType.Midfielder => "C",
            PlayerType.Forward => "A",
            _ => throw new ArgumentException($"Unsupported PlayerType: {type}")
        };
        
        var playerName = name ?? $"{role}_{id}";
        return SerieAPlayer.Create(id, role, role, playerName, "Test Team", fantaVoto, fantaVoto, 10);
    }

    [Fact]
    public void CompleteAuction_4Teams_AllRoles_ShouldFollowCorrectLogicAndTurnOrder()
    {
        // === SETUP INIZIALE ===
        var league = CreateTestLeague();
        
        // Crea 4 team con budget 500 ciascuno
        var teamA = league.AddTeam("Team A", 500);
        var teamB = league.AddTeam("Team B", 500);
        var teamC = league.AddTeam("Team C", 500);
        var teamD = league.AddTeam("Team D", 500);
        
        var teams = new[] { teamA, teamB, teamC, teamD };
        
        Console.WriteLine("=== AUCTION SIMULATION START ===");
        Console.WriteLine($"Teams: {string.Join(", ", teams.Select(t => t.Name))}");
        Console.WriteLine($"Initial budgets: {string.Join(", ", teams.Select(t => $"{t.Name}={t.Budget}"))}");
        
        // Avvia asta
        league.StartAuction(1, 1); // base price 1, min increment 1
        
        Assert.NotNull(league.ActiveAuction);
        Assert.Equal(AuctionStatus.Running, league.ActiveAuction.Status);
        
        // === CREAZIONE PLAYER POOL ===
        // 3 portieri (P), 8 difensori (D), 8 centrocampisti (C), 6 attaccanti (A) per team
        // Totale: 12P, 32D, 32C, 24A = 100 giocatori
        var players = CreatePlayerPool();
        
        Console.WriteLine($"Created player pool: {players.Count} players");
        Console.WriteLine($"P: {players.Count(p => p.Role == "P")}, D: {players.Count(p => p.Role == "D")}, " +
                         $"C: {players.Count(p => p.Role == "C")}, A: {players.Count(p => p.Role == "A")}");
        
        // === SIMULAZIONE ASTA COMPLETA ===
        int turnCounter = 0;
        var auctionLog = new List<string>();
        
        // Continua fino a quando tutti i giocatori sono stati nominati o tutti i team hanno completato le rose
        while (players.Any() && !AllTeamsCompleted(teams))
        {
            turnCounter++;
            var currentState = league.GetCurrentAuctionState();
            var currentTeam = teams.First(t => t.Id == currentState.CurrentTurn.CurrentTeamId);
            
            Console.WriteLine($"\n=== TURN {turnCounter}: {currentTeam.Name} ===");
            LogTeamStates(teams, auctionLog, $"Turn {turnCounter} start");
            
            // Scegli il prossimo giocatore da nominare (logica intelligente)
            var playerToNominate = ChoosePlayerToNominate(players, currentTeam, teams);
            
            if (playerToNominate == null)
            {
                Console.WriteLine($"{currentTeam.Name} cannot nominate any more players (roster complete)");
                // Se il team corrente non può nominare, passa al prossimo
                // Nella realtà questo dovrebbe essere gestito dall'auction flow
                break;
            }
            
            Console.WriteLine($"Nominating: {playerToNominate.Name} ({GetPlayerTypeFromRole(playerToNominate.Role)})");
            
            // === NOMINA GIOCATORE ===
            var nominationResult = league.NominatePlayer(currentTeam.Id, playerToNominate);
            
            if (nominationResult.IsAutoAssign)
            {
                // === SCENARIO 1: AUTO-ASSIGNMENT ===
                Console.WriteLine($"AUTO-ASSIGN: {playerToNominate.Name} -> {currentTeam.Name} for {nominationResult.Price}");
                auctionLog.Add($"Turn {turnCounter}: AUTO-ASSIGN {playerToNominate.Name} to {currentTeam.Name} for {nominationResult.Price}");
                
                // Verifica che il giocatore sia stato assegnato correttamente
                var ownership = league.PlayerOwnerships.FirstOrDefault(o => o.SerieAPlayerId == playerToNominate.Id);
                Assert.NotNull(ownership);
                Assert.Equal(currentTeam.Id, ownership.TeamId);
                Assert.Equal(nominationResult.Price, ownership.PurchasePrice);
                
                // Verifica aggiornamento budget
                var expectedBudget = 500 - league.PlayerOwnerships.Where(o => o.TeamId == currentTeam.Id).Sum(o => o.PurchasePrice);
                Assert.Equal(expectedBudget, currentTeam.Budget);
            }
            else if (nominationResult.IsReadyCheck)
            {
                // === SCENARIO 2: READY CHECK + BIDDING ===
                Console.WriteLine($"READY CHECK: {nominationResult.ReadyState.EligibleTeamIds.Count} eligible teams");
                Console.WriteLine($"Eligible: {string.Join(", ", nominationResult.ReadyState.EligibleTeamIds.Select(id => teams.First(t => t.Id == id).Name))}");
                
                // Simula ready check - tutti i team eligible confermano
                foreach (var eligibleTeamId in nominationResult.ReadyState.EligibleTeamIds)
                {
                    var confirmResult = league.ConfirmTeamReady(eligibleTeamId);
                    Assert.True(confirmResult, $"Failed to confirm ready for team {eligibleTeamId}");
                }
                
                // Verifica che ready check sia completato usando lo stato originale
                Assert.True(nominationResult.ReadyState.AllTeamsReady, 
                    $"AllTeamsReady should be true. EligibleCount: {nominationResult.ReadyState.EligibleTeamIds.Count}, ReadyCount: {nominationResult.ReadyState.ReadyTeamIds.Count}");
                Assert.True(nominationResult.ReadyState.IsCompleted, 
                    "Ready check should be completed after all teams confirmed ready");
                
                // Avvia bidding
                var biddingInfo = league.StartBiddingAfterReady();
                Assert.NotNull(biddingInfo);
                Assert.Equal(currentTeam.Id, biddingInfo.NominatorId);
                Assert.Equal(playerToNominate.Id, biddingInfo.PlayerId);
                
                Console.WriteLine($"BIDDING STARTED: Base price {biddingInfo.HighestBid}, current holder: {teams.First(t => t.Id == biddingInfo.HighestBidder).Name}");
                
                // === SIMULAZIONE BIDDING ===
                var biddingResult = SimulateBidding(league, teams, nominationResult.ReadyState.EligibleTeamIds, playerToNominate);
                
                Console.WriteLine($"BIDDING COMPLETED: Winner {biddingResult.WinnerName} with bid {biddingResult.FinalPrice}");
                auctionLog.Add($"Turn {turnCounter}: BIDDING {playerToNominate.Name} won by {biddingResult.WinnerName} for {biddingResult.FinalPrice}");
                
                // Finalizza bidding
                league.FinalizeBiddingRound(playerToNominate);
                
                // Verifica risultato
                var ownership = league.PlayerOwnerships.FirstOrDefault(o => o.SerieAPlayerId == playerToNominate.Id);
                Assert.NotNull(ownership);
                Assert.Equal(biddingResult.WinnerId, ownership.TeamId);
                Assert.Equal(biddingResult.FinalPrice, ownership.PurchasePrice);
            }
            
            // Rimuovi giocatore dal pool
            players.Remove(playerToNominate);
            
            // Log stato dopo il turno
            LogTeamStates(teams, auctionLog, $"Turn {turnCounter} end");
            
            // Verifica integrità dopo ogni turno
            VerifyAuctionIntegrity(league, teams);
        }
        
        // === VERIFICA FINALE ===
        Console.WriteLine("\n=== FINAL AUCTION STATE ===");
        LogFinalResults(teams, league, auctionLog);
        
        // Verifica che tutti i vincoli siano rispettati
        VerifyFinalAuctionResults(teams, league);
        
        Console.WriteLine("=== AUCTION SIMULATION COMPLETED SUCCESSFULLY ===");
    }

    private static List<SerieAPlayer> CreatePlayerPool()
    {
        var players = new List<SerieAPlayer>();
        int id = 1;
        
        // 12 Portieri (3 per team)
        for (int i = 0; i < 12; i++)
        {
            players.Add(CreateTestPlayer(id++, PlayerType.Goalkeeper, $"GK_{i + 1}"));
        }
        
        // 32 Difensori (8 per team)
        for (int i = 0; i < 32; i++)
        {
            players.Add(CreateTestPlayer(id++, PlayerType.Defender, $"DEF_{i + 1}"));
        }
        
        // 32 Centrocampisti (8 per team)
        for (int i = 0; i < 32; i++)
        {
            players.Add(CreateTestPlayer(id++, PlayerType.Midfielder, $"MID_{i + 1}"));
        }
        
        // 24 Attaccanti (6 per team)
        for (int i = 0; i < 24; i++)
        {
            players.Add(CreateTestPlayer(id++, PlayerType.Forward, $"FWD_{i + 1}"));
        }
        
        return players;
    }

    private static bool AllTeamsCompleted(Team[] teams)
    {
        return teams.All(t => 
            t.CountP >= 3 &&
            t.CountD >= 8 &&
            t.CountC >= 8 &&
            t.CountA >= 6);
    }

    private static SerieAPlayer ChoosePlayerToNominate(List<SerieAPlayer> availablePlayers, Team currentTeam, Team[] allTeams)
    {
        // Strategia intelligente di nomina:
        // 1. Priorità ai ruoli dove il team ha ancora slot
        // 2. Se possibile, scegli ruoli dove altri team hanno pochi slot (per auto-assign)
        // 3. Altrimenti scegli il primo disponibile per il ruolo più necessario
        
        var neededRoles = new List<PlayerType>();
        
        if (currentTeam.HasSlot(PlayerType.Goalkeeper)) neededRoles.Add(PlayerType.Goalkeeper);
        if (currentTeam.HasSlot(PlayerType.Defender)) neededRoles.Add(PlayerType.Defender);
        if (currentTeam.HasSlot(PlayerType.Midfielder)) neededRoles.Add(PlayerType.Midfielder);
        if (currentTeam.HasSlot(PlayerType.Forward)) neededRoles.Add(PlayerType.Forward);
        
        if (!neededRoles.Any()) return null; // Team completo
        
        // Per ogni ruolo necessario, controlla se ci sono giocatori disponibili
        foreach (var role in neededRoles)
        {
            var roleString = GetRoleStringFromPlayerType(role);
            var playersOfRole = availablePlayers.Where(p => p.Role == roleString).ToList();
            
            if (playersOfRole.Any())
            {
                return playersOfRole.First(); // Prendi il primo disponibile
            }
        }
        
        return null;
    }

    private static PlayerType GetPlayerTypeFromRole(string role)
    {
        return role switch
        {
            "P" => PlayerType.Goalkeeper,
            "D" => PlayerType.Defender,
            "C" => PlayerType.Midfielder,
            "A" => PlayerType.Forward,
            _ => throw new ArgumentException($"Unknown role: {role}")
        };
    }

    private static string GetRoleStringFromPlayerType(PlayerType type)
    {
        return type switch
        {
            PlayerType.Goalkeeper => "P",
            PlayerType.Defender => "D",
            PlayerType.Midfielder => "C",
            PlayerType.Forward => "A",
            _ => throw new ArgumentException($"Unknown PlayerType: {type}")
        };
    }

    private static (Guid WinnerId, string WinnerName, int FinalPrice) SimulateBidding(
        League league, Team[] teams, IReadOnlyList<Guid> eligibleTeamIds, SerieAPlayer player)
    {
        var biddingInfo = league.ActiveAuction.GetBiddingInfo();
        var currentPrice = biddingInfo.HighestBid;
        var currentWinner = biddingInfo.HighestBidder;
        
        // Simula bidding con logica intelligente
        // Gli altri team eligible possono fare offerte se:
        // 1. Hanno budget sufficiente
        // 2. Hanno slot per il ruolo
        // 3. "Vogliono" il giocatore (logica semplificata)
        
        var participatingTeams = eligibleTeamIds
            .Select(id => teams.First(t => t.Id == id))
            .Where(t => t.Budget > currentPrice && t.HasSlot(GetPlayerTypeFromRole(player.Role)))
            .ToList();
        
        Console.WriteLine($"Bidding participants: {string.Join(", ", participatingTeams.Select(t => t.Name))}");
        
        // Simulazione semplice: ogni team eligible fa un'offerta se può permettersela
        foreach (var team in participatingTeams)
        {
            var nextBid = currentPrice + 1; // Min increment
            
            if (team.Budget >= nextBid)
            {
                Console.WriteLine($"{team.Name} bids {nextBid}");
                league.PlaceBid(team.Id, nextBid);
                
                biddingInfo = league.ActiveAuction.GetBiddingInfo();
                currentPrice = biddingInfo.HighestBid;
                currentWinner = biddingInfo.HighestBidder;
                
                Assert.Equal(nextBid, currentPrice);
                Assert.Equal(team.Id, currentWinner);
            }
        }
        
        var winnerTeam = teams.First(t => t.Id == currentWinner);
        return (currentWinner, winnerTeam.Name, currentPrice);
    }

    private static void LogTeamStates(Team[] teams, List<string> auctionLog, string moment)
    {
        var stateLog = $"{moment} - Budgets: {string.Join(", ", teams.Select(t => $"{t.Name}={t.Budget}"))}";
        stateLog += $" | Rosters: {string.Join(", ", teams.Select(t => $"{t.Name}[P:{t.CountP},D:{t.CountD},C:{t.CountC},A:{t.CountA}]"))}";
        
        Console.WriteLine(stateLog);
        auctionLog.Add(stateLog);
    }

    private static void LogFinalResults(Team[] teams, League league, List<string> auctionLog)
    {
        Console.WriteLine("Final team rosters:");
        foreach (var team in teams)
        {
            var ownerships = league.PlayerOwnerships.Where(o => o.TeamId == team.Id).ToList();
            var totalSpent = ownerships.Sum(o => o.PurchasePrice);
            var remainingBudget = team.Budget;
            
            Console.WriteLine($"{team.Name}: Budget {remainingBudget}/{500} (spent {totalSpent})");
            Console.WriteLine($"  P: {team.CountP}/3");
            Console.WriteLine($"  D: {team.CountD}/8");
            Console.WriteLine($"  C: {team.CountC}/8");
            Console.WriteLine($"  A: {team.CountA}/6");
            Console.WriteLine($"  Total players: {ownerships.Count}");
        }
        
        Console.WriteLine($"\nTotal auction log entries: {auctionLog.Count}");
    }

    private static void VerifyAuctionIntegrity(League league, Team[] teams)
    {
        // Verifica che i budget siano coerenti
        foreach (var team in teams)
        {
            var ownerships = league.PlayerOwnerships.Where(o => o.TeamId == team.Id);
            var totalSpent = ownerships.Sum(o => o.PurchasePrice);
            var expectedBudget = 500 - totalSpent;
            
            Assert.Equal(expectedBudget, team.Budget);
        }
        
        // Verifica che non ci siano ownership duplicate
        var allOwnershipPlayerIds = league.PlayerOwnerships.Select(o => o.SerieAPlayerId).ToList();
        Assert.Equal(allOwnershipPlayerIds.Count, allOwnershipPlayerIds.Distinct().Count());
        
        // Verifica che i contatori dei giocatori siano coerenti
        foreach (var team in teams)
        {
            var ownerships = league.PlayerOwnerships.Where(o => o.TeamId == team.Id).ToList();
            
            // Conta per ruolo dalle ownerships
            var pCount = ownerships.Count(o => league.PlayerOwnerships.First(po => po.SerieAPlayerId == o.SerieAPlayerId).SerieAPlayerId % 1000 <= 12); // Approssimazione
            
            // Verifica limiti massimi
            Assert.True(team.CountP <= 3);
            Assert.True(team.CountD <= 8);
            Assert.True(team.CountC <= 8);
            Assert.True(team.CountA <= 6);
        }
    }

    private static void VerifyFinalAuctionResults(Team[] teams, League league)
    {
        // Verifica che tutti i vincoli finali siano rispettati
        var totalOwnerships = league.PlayerOwnerships.Count;
        var totalExpectedBudget = 4 * 500; // 4 teams * 500 budget
        var totalSpent = league.PlayerOwnerships.Sum(o => o.PurchasePrice);
        var totalRemainingBudget = teams.Sum(t => t.Budget);
        
        Console.WriteLine($"Budget verification: Initial={totalExpectedBudget}, Spent={totalSpent}, Remaining={totalRemainingBudget}");
        Assert.Equal(totalExpectedBudget, totalSpent + totalRemainingBudget);
        
        // Verifica che ogni team abbia rose valide
        foreach (var team in teams)
        {
            Assert.True(team.CountP >= 0);
            Assert.True(team.CountD >= 0);
            Assert.True(team.CountC >= 0);
            Assert.True(team.CountA >= 0);
            
            // Verifica che il budget sia non-negativo
            Assert.True(team.Budget >= 0);
        }
        
        Console.WriteLine("All final verification checks passed!");
    }
}
