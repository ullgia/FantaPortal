namespace Application.Events;

/// <summary>
/// Costanti per i nomi degli eventi SignalR.
/// Centralizza tutti i nomi degli eventi per evitare errori di battitura
/// e garantire consistenza tra client e server.
/// </summary>
public static class SignalREventNames
{
    // Eventi legacy (mantenuti per compatibilità)
    public const string AuctionStateChanged = "AuctionStateChanged";
    public const string BiddingTimerUpdate = "BiddingTimerUpdate";
    public const string NewHighestBid = "NewHighestBid";
    public const string ReadyCheckStarted = "ReadyCheckStarted";
    public const string BiddingStarted = "BiddingStarted";
    public const string TurnAdvanced = "TurnAdvanced";
    
    // Eventi ottimizzati con DTOs completi
    public const string TurnOrderUpdate = "TurnOrderUpdate";
    public const string PlayerNominated = "PlayerNominated";
    public const string ReadyStatesUpdate = "ReadyStatesUpdate";
    public const string BidUpdate = "BidUpdate";
    public const string FullStateUpdate = "FullStateUpdate";
    public const string PhaseChanged = "PhaseChanged";
    
    // Eventi di sistema
    public const string ConnectionEstablished = "ConnectionEstablished";
    public const string ConnectionLost = "ConnectionLost";
    public const string ErrorOccurred = "ErrorOccurred";
    
    // Eventi di gruppo
    public const string UserJoinedGroup = "UserJoinedGroup";
    public const string UserLeftGroup = "UserLeftGroup";
    public const string GroupUpdate = "GroupUpdate";
}
