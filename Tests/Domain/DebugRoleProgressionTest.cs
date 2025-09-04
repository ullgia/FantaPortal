using System;
using System.Collections.Generic;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Xunit;

namespace Tests.Domain
{
    public class DebugRoleProgressionTest
    {
        private static LeaguePlayer CreateTeamWithSlots(PlayerType role, int availableSlots)
        {
            var leagueId = Guid.NewGuid();
            var team = LeaguePlayer.CreateInternal(leagueId, $"TestTeam", 5000);
            
            // Simula l'uso di slot assegnando giocatori usando l'API pubblica
            var maxSlots = role switch
            {
                PlayerType.Goalkeeper => 3,  // Max 3 portieri
                PlayerType.Defender => 8,    // Max 8 difensori
                PlayerType.Midfielder => 8,  // Max 8 centrocampisti
                PlayerType.Forward => 6,     // Max 6 attaccanti
                _ => 0
            };

            var usedSlots = maxSlots - availableSlots;

            // Usa AssignPlayerInternal per simulare giocatori già assegnati
            for (int i = 0; i < usedSlots; i++)
            {
                try
                {
                    team.AssignPlayerInternal(role, 10); // Prezzo fisso per test
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to assign player {i+1}/{usedSlots} for role {role}: {ex.Message}");
                }
            }

            return team;
        }

        [Fact]
        public void Debug_RoleProgression_TestSlotLogic()
        {
            // Arrange
            var team1 = CreateTeamWithSlots(PlayerType.Goalkeeper, 0); // Non ha slot P
            
            // Verifica stato del team1 per Goalkeeper
            var counts = team1.GetPlayerCounts();
            var availableP = team1.GetAvailableSlots(PlayerType.Goalkeeper);
            var hasSlotP = team1.HasSlot(PlayerType.Goalkeeper);
            
            Console.WriteLine($"Team1 Goalkeeper: CountP={counts.P}, AvailableP={availableP}, HasSlotP={hasSlotP}");
            
            // Aggiungiamo slot per difensori (max 8, usiamo 7 per avere 1 disponibile)
            for (int i = 0; i < 7; i++)
                team1.AssignPlayerInternal(PlayerType.Defender, 10);
                
            var availableD = team1.GetAvailableSlots(PlayerType.Defender);
            var hasSlotD = team1.HasSlot(PlayerType.Defender);
            Console.WriteLine($"Team1 Defender: AvailableD={availableD}, HasSlotD={hasSlotD}");
            
            var teamOrder = new List<Guid> { team1.Id };
            var teams = new Dictionary<Guid, LeaguePlayer> { { team1.Id, team1 } };

            // Act
            var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Goalkeeper, 0);

            // Debug output
            Console.WriteLine($"Input: currentRole={PlayerType.Goalkeeper}, currentIndex=0");
            Console.WriteLine($"Result: nextRole={nextRole}, nextIndex={nextIndex}");
            
            // The logic should be:
            // 1. Check from currentIndex+1 to end in same role (Goalkeeper) - should be none
            // 2. Check from 0 to currentIndex in same role (Goalkeeper) - should be none since currentIndex=0
            // 3. Move to next role (Defender) and check all teams
            
            // Assert
            Assert.Equal(PlayerType.Defender, nextRole);
            Assert.Equal(0, nextIndex);
        }
    }
}
