---
applyTo: '**'
---

# FantaPortal Project Memory

## Project Overview
- **Type**: .NET 9 Blazor SSR application for fantasy football auction management
- **Architecture**: Clean Architecture with DDD principles
- **Main Components**: Domain layer, Application layer, Infrastructure layer, Portal (UI)
- **Key Features**: Real-time auction management, timer system, authentication, team management

## Recent Implementation Details

### Enhanced Authentication System (MagicLink)
- **Domain Entity**: `MagicLink` with secure token-based authentication
- **Purpose**: Allows participants, spectators, and admins to access auctions via secure links
- **Implementation**: Factory methods for different user types, secure token generation, expiry validation
- **Location**: `Domain/Entities/MagicLink.cs`, `Domain/Enums/MagicLinkType.cs`

### Persistent Timer System
- **Domain Entity**: `PersistedTimer` for database-backed auction timers
- **Purpose**: Ensures timer state survives application restarts and provides recovery mechanisms
- **Features**: Pause/resume functionality, automatic expiry detection, remaining time calculation
- **Location**: `Domain/Entities/PersistedTimer.cs`

### Event Reliability (OutboxEvent Pattern)
- **Domain Entity**: `OutboxEvent` for guaranteed event delivery
- **Purpose**: Ensures domain events are not lost during system failures
- **Implementation**: Event serialization, retry mechanisms, delivery tracking
- **Location**: `Domain/Entities/OutboxEvent.cs`, `Domain/Enums/AuditAction.cs`

### Enhanced Timer Manager
- **Service**: `ImprovedAuctionTimerManager` with recovery capabilities
- **Location**: `Infrastructure/Services/ImprovedAuctionTimerManager.cs`
- **Features**: Database persistence, automatic recovery after restart, background recovery timer
- **Integration**: Implements `IHostedService` for lifecycle management

## Database Configuration
- **EF Core Configurations**: Created for all new entities with proper constraints and relationships
- **Location**: `Infrastructure/Persistence/Configurations/`
- **Status**: Entities configured, migration pending

## Architectural Decisions
- **Layer Separation**: Strict adherence to DDD principles - Infrastructure services handle database access
- **Event Publishing**: Domain events published through `IDomainEventPublisher` interface
- **Dependency Injection**: All services properly registered in their respective layers
- **Error Handling**: Domain exceptions used for business rule violations

## Next Steps
1. Create database migration for new entities
2. Consider replacing original timer manager with enhanced version
3. Implement UI components for MagicLink authentication
4. Test timer recovery mechanisms in production scenarios
