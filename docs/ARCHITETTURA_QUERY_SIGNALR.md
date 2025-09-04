# Architettura Query e SignalR per Ridurre Chiamate Multiple

## Problema Risolto

Il problema iniziale era che ogni componente UI faceva query separate, creando:
- Query multiple per gli stessi dati
- Carico eccessivo sul database
- Latenza alta nell'interfaccia
- Difficoltà a sincronizzare i dati via SignalR

## Soluzione Implementata

### 1. DTO Ottimizzati per l'UI

**Prima** - Ogni componente faceva query separate:
```csharp
// AuctionView faceva:
GetCurrentAuctionStateAsync() // Per stato base
GetTeamsSummaryAsync()        // Per teams info
GetLeagueStatsAsync()         // Per league info

// TurnOrderPanel faceva:
GetCurrentAuctionStateAsync() // Solo per il ruolo corrente
// + Mock data per turn order

// ReadyCheckSection faceva:
GetReadyStatesAsync()         // Per ready states
GetCurrentAuctionStateAsync() // Per total teams
```

**Dopo** - Query unificate:
```csharp
// AuctionOverviewDto: include tutto quello che serve per la vista principale
public record AuctionOverviewDto(
    Guid AuctionId,
    string LeagueName,
    AuctionStatus Status,
    RoleType CurrentRole,
    int CurrentTurnPosition,
    int TotalTeams,
    Guid CurrentTurnTeamId,
    string CurrentTurnTeamName,
    IReadOnlyList<TurnOrderDto> TurnOrder,    // ← Include turn order completo
    bool IsBiddingActive,
    bool IsReadyCheckActive
);

// TurnOrderDto: ottimizzato per UI
public record TurnOrderDto(
    int Position,
    Guid TeamId,
    string TeamName,
    bool IsCurrentTurn    // ← Calcolato server-side
);
```

### 2. Query Methods Ottimizzati

```csharp
// Una query che prende tutto
Task<AuctionOverviewDto?> GetAuctionOverviewAsync(Guid leagueId, CancellationToken ct = default);

// Query specifica per turn order (usata da SignalR per aggiornamenti)
Task<IReadOnlyList<TurnOrderDto>> GetTurnOrderAsync(Guid leagueId, CancellationToken ct = default);
```

### 3. Strategia SignalR per Aggiornamenti Incrementali

```csharp
// DTO ottimizzati per eventi SignalR
public record PlayerNominatedDto(
    int PlayerId,
    string PlayerName,
    RoleType Role,
    string Team,
    decimal FVM,
    Guid NominatingTeamId,
    string NominatingTeamName
);

public record PlayerNominationEvent(
    int PlayerId,
    string PlayerName,
    Domain.Enums.RoleType Role,
    string Team,
    decimal FVM,
    Guid NominatingTeamId,
    string NominatingTeamName
);

// Hub methods ottimizzati
public class AuctionHub : Hub
{
    // Per aggiornamenti completi (rare)
    public async Task RefreshFullState(Guid sessionId)
    {
        var overview = await _queries.GetAuctionOverviewAsync(sessionId);
        await Clients.Group(SessionGroup(sessionId)).SendAsync("FullStateUpdate", overview);
    }

    // Per aggiornamenti incrementali (frequenti)
    public async Task OnTurnAdvanced(Guid sessionId, int newPosition, Guid newTeamId)
    {
        var turnOrder = await _queries.GetTurnOrderAsync(sessionId);
        await Clients.Group(SessionGroup(sessionId)).SendAsync("TurnOrderUpdate", turnOrder);
    }

    public async Task OnBidPlaced(Guid sessionId, BidDto bid)
    {
        // Solo i dati del bid, non tutto lo stato
        await Clients.Group(SessionGroup(sessionId)).SendAsync("BidUpdate", bid);
    }

    public async Task OnPlayerNominated(Guid sessionId, PlayerNominatedDto playerData)
    {
        // Include tutte le info necessarie: nome, team, FVM
        await Clients.Group(SessionGroup(sessionId)).SendAsync("PlayerNominated", playerData);
    }
}

// Interface ottimizzata per evitare query multiple nei client
public interface IRealtimeNotificationService
{
    // Eventi con dati completi per evitare query sui client
    Task NotifyPlayerNominated(Guid leagueId, PlayerNominatedDto playerNominated);
    Task BiddingReadyRequested(Guid sessionId, PlayerNominationEvent playerNomination, IReadOnlyList<Guid> eligibleOtherTeamIds);
    Task BiddingReadyCompleted(Guid sessionId, PlayerNominationEvent playerNomination, IReadOnlyList<Guid> eligibleOtherTeamIds);
    
    // Altri eventi incrementali
    Task NotifyTurnOrderChanged(Guid leagueId, IReadOnlyList<TurnOrderDto> turnOrder);
    Task NotifyReadyStatesChanged(Guid leagueId, IReadOnlyList<ReadyStateDto> readyStates);
    Task NotifyBidPlaced(Guid leagueId, BidDto bid);
}
```
```

### 4. Component Pattern Ottimizzato

**AuctionView.razor** - Hub principale:
```csharp
protected override async Task OnInitializedAsync()
{
    // UNA sola query iniziale
    var overview = await AuctionQueries.GetAuctionOverviewAsync(AuctionId);
    
    // Configura SignalR per aggiornamenti incrementali
    await InitializeSignalR();
}

// SignalR callbacks
private async Task OnTurnOrderUpdate(IReadOnlyList<TurnOrderDto> newTurnOrder)
{
    // Aggiorna solo i dati necessari senza re-query
    currentTurn = newTurnOrder.FirstOrDefault(t => t.IsCurrentTurn)?.Position ?? 1;
    await turnOrderPanel.UpdateTurnOrder(newTurnOrder);
}
```

**ReadyCheckSection.razor** - Componente con eventi ottimizzati:
```csharp
// Variabile aggiornata per ricevere dati completi
private PlayerNominatedDto? nominatedPlayer;

// SignalR callbacks con dati completi
private async Task OnPlayerNominated(PlayerNominatedDto playerData)
{
    // Aggiornamento diretto con tutti i dati necessari
    nominatedPlayer = playerData;
    await InvokeAsync(StateHasChanged);
}

private async Task OnReadyStatesUpdate(IReadOnlyList<ReadyStateDto> readyStates)
{
    // Aggiornamento diretto senza query
    readyTeams = readyStates.Where(r => r.IsReady).Select(r => r.TeamName).ToList();
    isCurrentTeamReady = readyStates.Any(r => r.TeamId == CurrentTeamId && r.IsReady);
    await InvokeAsync(StateHasChanged);
}

// Template della UI con dati completi
@if (nominatedPlayer != null)
{
    <span>Giocatore nominato: <strong>@nominatedPlayer.PlayerName</strong> (@nominatedPlayer.Team) - FVM: @nominatedPlayer.FVM</span>
}
```

## Benefici dell'Architettura

### 1. Riduzione Query Database
- **Prima**: 3-5 query per caricare una vista
- **Dopo**: 1 query per caricare + aggiornamenti incrementali via SignalR

### 2. Performance UI
- Caricamento iniziale più veloce (1 query invece di N)
- Aggiornamenti real-time senza polling
- Dati consistenti tra tutti i client

### 3. Scalabilità
- Meno carico sul database
- SignalR gestisce la distribuzione dei cambiamenti
- Cache-friendly (i DTO sono immutabili)

## Pattern per Nuovi Componenti

1. **Identifica i dati necessari** per il componente
2. **Crea un DTO dedicato** che include tutto in una struttura flat
3. **Implementa una query unificata** nel service
4. **Usa SignalR per aggiornamenti** incrementali
5. **Evita query multiple** nel componente

### Esempio Template:
```csharp
// 1. DTO per il componente
public record MyComponentDto(
    // Tutti i dati necessari per il rendering
);

// 2. Query unificata
Task<MyComponentDto?> GetMyComponentDataAsync(Guid id, CancellationToken ct = default);

// 3. Componente
protected override async Task OnInitializedAsync()
{
    data = await Service.GetMyComponentDataAsync(Id);
    await ConfigureSignalR();
}

// 4. SignalR update
private async Task OnDataUpdate(MyComponentDto newData)
{
    data = newData;
    StateHasChanged();
}
```

## Monitoring e Debug

Per monitorare l'efficacia:
1. **Log delle query** nel Infrastructure layer
2. **Metriche SignalR** per messaggi inviati/ricevuti  
3. **Performance browser** per tempi di caricamento
4. **Database query profiling** per identificare N+1 queries

Questa architettura garantisce che l'applicazione sia:
- ✅ **Veloce**: Meno query, aggiornamenti incrementali
- ✅ **Consistente**: Stato sincronizzato via SignalR
- ✅ **Scalabile**: Architettura query-efficient
- ✅ **Manutenibile**: Pattern chiari e riutilizzabili
