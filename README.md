# Specifiche Funzionali - Sistema Aste Fantacalcio

## 1. Panoramica del Sistema

### 1.1 Scopo del Sistema
Sistema web per gestire aste di fantacalcio in tempo reale che permette a gruppi di giocatori di organizzare aste per l'acquisto di calciatori della Serie A, con gestione di budget, timer, turni e comunicazione in tempo reale.

### 1.2 Architettura Tecnica
- **Framework**: Blazor Server-Side Rendering (SSR) con .NET 9
- **Comunicazione Real-time**: SignalR per notifiche e aggiornamenti immediati
- **Sessioni**: Una sola sessione d'asta attiva per lega alla volta (per tipo: assegnazione, svincoli, riparazione)
- **Interfaccia Utenti**: Pagina unica semplificata per partecipanti
- **Interfaccia Master**: Dashboard di gestione e controllo dedicata
- **Autenticazione Differenziata**: Login completo per master, magic link per partecipanti
- **Architettura**: Domain-Driven Design (DDD) con Rich Domain Model
- **Persistenza**: Event-driven con registrazione di ogni comando a database
- **Struttura**: NO MEDIATOR, NO AUTOMAPPER O MAPPER IN GENERALE, SI A ENTITà direttamente usate nelle pagine, SI a funzioni di dominio nelle chiamate del dominio per validare la logica di funzionamento, Si eventi di dominio con dispatcher in ef core interceptor per usare outbox pattern con pubblicazioni di eventi di dominio
### 1.3 Attori Principali e Autenticazione
- **Master/Amministratore**: Utente registrato con login completo (email/password)
  - Crea e configura leghe tramite dashboard dedicata
  - Gestisce aste e monitoraggio in tempo reale
  - Account permanente con storico delle leghe
- **Partecipante**: Accesso tramite magic link senza registrazione
  - Partecipa alle aste tramite interfaccia semplificata
  - Identificato solo dal nome nella lega specifica
  - Accesso temporaneo limitato alla sessione asta
- **Sistema**: Gestisce automaticamente timer, turni e validazioni

### 1.4 Funzionalità Principali
- Creazione e gestione leghe fantacalcio
- Configurazione e avvio aste in tempo reale
- Sistema di turni alternati per nominazione giocatori
- Meccanismo di offerte intelligente con assegnazione automatica
- Gestione budget e rosa giocatori
- Comunicazione real-time tramite SignalR
- Sistema svincoli e aste di riparazione

## 2. Gestione Leghe

### 2.1 Creazione Lega (Master)
- Il master (utente registrato) crea una nuova lega specificando:
  - Nome della lega
  - Numero di partecipanti (tipicamente 8-12)
  - Budget iniziale per partecipante (es. 500 crediti)
  - Configurazione rosa: numero giocatori per ruolo (P/D/C/A)
  - Regole specifiche dell'asta

### 2.2 Sistema Inviti e Accessi
- **Generazione Magic Link**: Master crea link univoci per ogni partecipante
- **Accesso Partecipanti**: 
  - Clic su magic link per accesso diretto senza registrazione
  - Identificazione tramite nome assegnato nella lega
  - Sessione temporanea legata al link specifico
- **Gestione Presenze**: Master monitora connessioni dalla dashboard

### 2.3 Autenticazione Differenziata
- **Master**: 
  - Registrazione completa con email e password
  - Account permanente con storico leghe
  - Accesso dashboard amministrativa
- **Partecipanti**:
  - Accesso tramite magic link unico
  - Nessuna registrazione richiesta
  - Identificazione solo per nome nella lega
  - Sessione limitata alla durata dell'asta

### 2.4 Configurazione Rosa
- **Portieri (P)**: numero massimo acquistabili
- **Difensori (D)**: numero massimo acquistabili  
- **Centrocampisti (C)**: numero massimo acquistabili
- **Attaccanti (A)**: numero massimo acquistabili
- Validazione che la somma rispetti i limiti della lega

## 3. Sistema Aste

### 3.1 Gestione Database Giocatori
- **Caricamento da CSV**: Sistema di importazione file strutturato per giocatori
- **Struttura File CSV**:
  - ID giocatore, Nome, Cognome, Ruolo, Squadra di appartenenza
  - Foto giocatore (URL o path), Prezzo base, Status
  - Campo per identificare giocatori che hanno lasciato il campionato
- **Operazioni Supportate**:
  - Inserimento nuovi giocatori
  - Aggiornamento dati giocatori esistenti
  - Marcatura giocatori che hanno lasciato il campionato
  - Gestione automatica svincoli per giocatori non più disponibili

### 3.2 Gestione Giocatori Non Disponibili
- **Rilevamento Automatico**: Sistema identifica giocatori che hanno lasciato il campionato
- **Svincolo Automatico**: 
  - Rilascio automatico da tutte le squadre che li possiedono
  - Rimborso al valore di mercato attuale (non prezzo di acquisto)
  - Notifica automatica ai proprietari delle squadre
- **Segnalazione Conflitti**: Alert per giocatori presenti in liste d'attesa o pre-selezioni
- **Log Operazioni**: Tracciamento completo di tutti gli svincoli automatici

### 3.3 Preparazione Asta
- **Lista Giocatori Disponibili**: Solo giocatori attivi e presenti nel campionato
- **Filtri Automatici**: Esclusione automatica giocatori non disponibili
- **Validazione**: Controllo integrità dati prima dell'avvio asta
- **Backup**: Salvataggio stato precedente prima dell'importazione

### 3.4 Configurazione Asta
- **Timer Configurabili**:
  - Tempo per nominazione (es. 30 secondi)
  - Tempo per offerte dopo nominazione (es. 60 secondi)
  - Tempo aggiuntivo per controproposte (es. 10 secondi)
- **Regole di Incremento**:
  - Offerta minima di partenza (es. 1 credito)
  - Incremento minimo tra offerte (es. 1 credito)
- **Ordine Turni**: Randomizzazione o definizione manuale

### 3.5 Avvio e Gestione Asta
- Verifica presenza di tutti i partecipanti
- Avvio automatico quando tutti confermano
- Possibilità di pausa/riavvio dell'asta
- Controllo amministratore per forzare turni

## 4. Flusso Turni e Nominazioni

### 4.1 Sequenza Turno Standard
1. **Identificazione Nominatore**: Il sistema indica chi deve nominare
2. **Fase Nominazione**: 
   - Il nominatore seleziona un giocatore dalla lista
   - Validazione che il giocatore sia disponibile e compatibile
3. **Controllo Slot Disponibili**:
   - Sistema verifica quanti giocatori hanno ancora slot per quel ruolo
   - Se solo il nominatore ha slot → Assegnazione automatica a 1 credito
   - Se altri giocatori hanno slot → Procede alla fase offerte
4. **Fase Offerte** (solo se necessaria):
   - Conferma partecipanti pronti (ready-check)
   - Avvio timer per le offerte
   - Raccolta offerte dai partecipanti
   - Aggiornamento in tempo reale dell'offerta più alta
5. **Finalizzazione**:
   - Assegnazione giocatore al miglior offerente (o automatica)
   - Aggiornamento budget e rosa
   - Avanzamento al turno successivo

### 4.2 Gestione Assegnazione Automatica
- **Condizione**: Solo il nominatore ha slot disponibili per il ruolo del giocatore nominato
- **Azione**: Giocatore assegnato automaticamente a 1 credito
- **Notifica**: Comunicazione immediata a tutti i partecipanti
- **Avanzamento**: Passaggio diretto al turno successivo senza timer

### 4.3 Progressione Intelligente per Ruolo
- **Controllo Globale**: Prima di assegnare un turno, verifica slot disponibili
- **Salto Automatico**: Se nessun giocatore può acquistare un ruolo, passa al tipo successivo
- **Rotazione Completa**: Quando tutti i giocatori hanno completato tutti i ruoli
- **Completamento Asta**: Passaggio alla fase di revisione finale

## 5. Sistema Offerte

### 5.1 Meccanica Offerte
- **Offerta Iniziale**: Prezzo base configurabile (es. 1 credito)
- **Incrementi**: Offerte successive devono superare di almeno X crediti
- **Validazioni**:
  - Controllo budget disponibile
  - Verifica slot disponibili nel ruolo
  - Impedire offerte superiori al budget rimanente

### 5.2 Timer e Scadenze
- **Timer Principale**: Countdown per l'intera fase offerte
- **Estensione Automatica**: Se offerta negli ultimi secondi, aggiunta tempo
- **Avvisi Temporali**: Notifiche a 30s, 10s, 5s dalla scadenza
- **Finalizzazione Automatica**: Allo scadere del timer

### 5.3 Interfaccia Offerte
- Visualizzazione offerta corrente più alta
- Storico ultime offerte con timestamp
- Pulsanti rapidi per incrementi standard (+1, +5, +10)
- Campo libero per offerta personalizzata
- Indicatori visivi del tempo rimanente

## 6. Gestione Budget e Rosa

### 6.1 Budget Giocatori
- **Budget Iniziale**: Crediti assegnati all'inizio (es. 500)
- **Spesa Corrente**: Aggiornamento in tempo reale dopo ogni acquisto
- **Budget Residuo**: Visualizzazione crediti rimanenti
- **Vincoli**: Impossibilità di fare offerte superiori al budget

### 6.2 Composizione Rosa
- **Contatori per Ruolo**: Visualizzazione giocatori acquistati/disponibili
- **Lista Acquisti**: Elenco giocatori già in rosa con prezzi
- **Stato Completamento**: Indicatori quando un ruolo è completo
- **Validazioni**: Controllo limiti massimi per ruolo

### 6.3 Calcoli Automatici
- **Spesa Media Rimanente**: Budget diviso slot residui
- **Suggerimenti**: Indicazioni su budget consigliato per slot
- **Proiezioni**: Stima completamento rosa con budget attuale

## 7. Comunicazione Real-Time

### 7.1 Eventi Automatici
- **Nuova Nominazione**: Notifica a tutti i partecipanti
- **Nuova Offerta**: Aggiornamento immediato offerta più alta
- **Cambio Stato**: Notifiche passaggio tra fasi (nominazione→offerte→finalizzazione)
- **Timer**: Aggiornamenti countdown in tempo reale
- **Acquisto**: Conferma assegnazione giocatore

### 7.2 Notifiche Utente
- **Turno Personale**: Avviso quando è il proprio turno di nominare
- **Offerta Superata**: Notifica quando qualcuno supera la propria offerta
- **Timer in Scadenza**: Avvisi urgenti negli ultimi secondi
- **Completamento Ruoli**: Notifica quando si completa un ruolo

### 7.3 Indicatori di Presenza
- **Stato Online**: Visualizzazione giocatori connessi/disconnessi
- **Ready Status**: Indicatori di conferma preparazione per turno
- **Attività**: Segnalazione ultima azione di ogni giocatore

## 8. Gestione Sessioni e Interfaccia Unica

### 8.1 Sessione Asta Unica
- **Limitazione**: Una sola sessione d'asta attiva per lega alla volta
- **Accesso Esclusivo**: Gli utenti possono accedere solo alla sessione corrente
- **Controllo Stato**: Prevenzione di aste multiple simultanee

### 8.2 Interfaccia Unificata
- **Pagina Unica**: Tutti i controlli e informazioni in una sola schermata
- **Dashboard Integrata**: Visualizzazione completa di:
  - Stato asta corrente e turno attivo
  - Budget personale e rosa in costruzione
  - Timer e countdown attivi
  - Area di nominazione e offerte
- **Navigazione Laterale**: Pulsanti per visualizzare:
  - Squadre degli altri partecipanti
  - Statistiche e classifiche temporanee
  - Lista giocatori disponibili con filtri

### 8.3 Fase di Completamento e Revisione
- **Rilevamento Completamento**: Sistema verifica quando tutti hanno completato le rose
- **Schermata Anteprima**: Visualizzazione completa di tutte le squadre formate
- **Possibilità Correzioni**: Interfaccia per modifiche dell'ultimo minuto
- **Conferma Finale**: Approvazione definitiva dell'asta da parte di tutti

## 9. Sistema Svincoli e Gestione Giocatori

### 9.1 Svincoli Automatici da Aggiornamento CSV
- **Rilevamento**: Sistema identifica giocatori che hanno lasciato il campionato dal file CSV
- **Svincolo Automatico**: 
  - Rilascio immediato da tutte le squadre proprietarie
  - Rimborso al valore di mercato attuale del giocatore
  - Nessun rimborso basato sul prezzo di acquisto originale
- **Notifiche**: 
  - Alert automatici ai proprietari delle squadre interessate
  - Comunicazione dell'importo rimborsato
  - Log delle operazioni per trasparenza
- **Gestione Conflitti**: Segnalazione giocatori in liste d'attesa o pre-selezioni

### 9.2 Gestione Svincoli Manuali
- **Periodo Svincoli**: Finestra temporale configurabile per rilasci volontari
- **Lista Svincolati**: Gestione giocatori messi volontariamente sul mercato
- **Regole**: Limitazioni e tempistiche per gli svincoli volontari
- **Budget Recovery**: Recupero basato su regole specifiche della lega

### 9.3 Aste di Riparazione
- **Attivazione**: Meccanismo per aste supplementari dopo svincoli
- **Partecipanti**: Solo giocatori con slot disponibili per i ruoli interessati
- **Modalità**: Asta semplificata con regole specifiche
- **Integrazione**: Utilizzo della stessa interfaccia dell'asta principale

## 10. Interfacce e Controlli di Sistema

### 10.1 Interfaccia Master - Dashboard di Gestione
- **Accesso Dedicato**: Dashboard separata per il master della lega
- **Controlli Amministrativi**:
  - Pausa/Riavvio asta in tempo reale
  - Forzatura turni e avanzamento manuale
  - Gestione partecipanti (rimozione/aggiunta)
  - Modifica timer e parametri durante l'asta
  - Undo/Redo per correzione errori
- **Monitoraggio Completo**:
  - Visualizzazione stato di tutti i giocatori
  - Assegnazioni in tempo reale
  - Log eventi dettagliato
  - Statistiche live (tempo medio, offerte, budget)
- **Gestione Emergenze**:
  - Controlli per situazioni problematiche
  - Backup e recovery dello stato
  - Risoluzione dispute

### 10.2 Interfaccia Utenti - Pagina Unificata
- **Accesso Partecipanti**: Interfaccia semplificata e focalizzata
- **Funzionalità Integrate**:
  - Dashboard personale con budget e rosa
  - Area nominazione e offerte
  - Visualizzazione turno corrente
  - Timer e countdown in tempo reale
- **Navigazione Laterale**:
  - Squadre degli altri partecipanti
  - Statistiche personali
  - Lista giocatori disponibili
- **Aggiornamenti Automatici**: Sincronizzazione SignalR senza controlli amministrativi

### 10.3 Separazione Funzionalità
- **Master**: Controllo completo dell'asta, gestione eventi, risoluzione problemi
- **Utenti**: Partecipazione attiva ma senza controlli di sistema
- **Sicurezza**: Autorizzazioni differenziate per ruoli
- **Interfacce Dedicate**: Design ottimizzato per ogni tipologia di utilizzo

## 11. Esperienza Utente e Interfacce Differenziate

### 11.1 Interfaccia Utenti - Blazor SSR Unificata
- **Tecnologia**: Blazor Server-Side Rendering con .NET 9
- **Pagina Unica**: Dashboard completa per partecipanti con tutte le funzionalità integrate
- **Aggiornamenti Real-time**: SignalR per sincronizzazione immediata
- **Componenti Principali per Utenti**:
  - Dashboard centrale con stato asta e turno corrente
  - Pannello budget e rosa personale
  - Area nominazione con selezione giocatori
  - Sezione offerte (quando necessaria)
  - Pannelli laterali per visualizzazione squadre altre

### 11.2 Interfaccia Master - Dashboard di Controllo
- **Accesso Separato**: Interfaccia dedicata per il master della lega
- **Funzionalità Avanzate**:
  - Panoramica completa di tutti i partecipanti
  - Controlli per gestione eventi in tempo reale
  - Visualizzazione dettagliata assegnazioni giocatori
  - Monitoraggio stato sistema e performance
- **Strumenti di Gestione**:
  - Log eventi in tempo reale
  - Statistiche complete dell'asta
  - Controlli di emergenza e recovery
  - Interfaccia per correzioni e modifiche

### 11.3 Navigazione e Responsive Design
- **Utenti - Vista Semplificata**:
  - Interfaccia ottimizzata per partecipazione
  - Pulsanti laterali per squadre e statistiche
  - Modalità responsive per tutti i dispositivi
- **Master - Vista Completa**:
  - Dashboard multi-pannello per controllo totale
  - Interfaccia desktop-first per gestione complessa
  - Strumenti avanzati per amministrazione
- **Sincronizzazione**: Aggiornamenti SignalR real-time su entrambe le interfacce

## 12. Stati e Transizioni del Sistema

### 12.1 Stati Asta
- **Preparazione**: Configurazione e attesa partecipanti
- **In Corso**: Asta attiva con turni in progressione
- **Pausa**: Sospensione temporanea amministratore
- **Revisione**: Fase anteprima finale con possibilità correzioni
- **Completata**: Tutte le rose completate e confermate
- **Annullata**: Interruzione definitiva dell'asta

### 12.2 Stati Turno
- **In Attesa Nominazione**: Aspetta selezione giocatore dal nominatore
- **Controllo Slot**: Sistema verifica disponibilità slot per il ruolo
- **Assegnazione Automatica**: Solo nominatore ha slot disponibili
- **Ready-check**: Conferma partecipanti per fase offerte
- **Fase Offerte**: Raccolta offerte attiva con timer
- **Finalizzato**: Giocatore assegnato, avanzamento turno

### 12.3 Stati Giocatore nella Sessione
- **Connesso/Attivo**: Partecipante online e disponibile
- **Connesso/Non Pronto**: Online ma non ha confermato ready
- **Disconnesso**: Temporaneamente non disponibile
- **Completato**: Ha riempito tutti i slot della rosa
- **In Attesa**: Non è il suo turno di nominare

## 13. Regole di Business Specifiche

### 13.1 Logica Assegnazione Automatica
- **Condizione**: Solo il nominatore ha slot disponibili per il ruolo selezionato
- **Azione**: Assegnazione immediata a 1 credito senza timer
- **Notifica**: Comunicazione SignalR a tutti i partecipanti
- **Progressione**: Passaggio automatico al turno successivo

### 13.2 Gestione Progressione Ruoli
- **Controllo Globale**: Verifica slot disponibili prima di ogni assegnazione turno
- **Salto Tipo**: Se nessuno può acquistare un ruolo, passa al tipo successivo
- **Ciclo Completo**: Quando tutti i tipi sono completati, attiva fase revisione
- **Validazione**: Controllo che ogni partecipante abbia almeno un giocatore per ruolo

### 13.3 Vincoli Sessione Unica
- **Limitazione Concorrenza**: Una sola asta attiva per lega alla volta
- **Accesso Esclusivo**: Redirect automatico alla sessione corrente
- **Prevenzione Conflitti**: Blocco creazione nuove aste se una è in corso
- **Recovery**: Possibilità di ripristino in caso di interruzioni
- **Dispute**: Procedure per gestire contestazioni
- **Abbandono**: Gestione giocatori che escono dall'asta

## 12. Requisiti Tecnici

### 12.1 Performance
- **Latenza**: Aggiornamenti real-time sotto i 100ms
- **Concorrenza**: Supporto fino a 12 giocatori simultanei per asta
- **Affidabilità**: Uptime 99.9% durante le aste
- **Scalabilità**: Supporto multiple aste contemporanee

### 12.2 Sicurezza e Autenticazione
- **Master**: 
  - Autenticazione completa con email/password
  - Sessioni sicure con JWT o cookie autenticati
  - Autorizzazioni complete per gestione leghe
- **Partecipanti**:
  - Accesso tramite magic link con token temporaneo
  - Autorizzazioni limitate alle azioni di partecipazione
  - Sessioni temporanee legate al link specifico
- **Integrità Dati**: Validazione server-side di tutte le operazioni
- **Audit Trail**: Log completo per tracciabilità azioni

### 12.3 Compatibilità
- **Browser**: Chrome, Firefox, Safari, Edge (ultime 2 versioni)
- **Dispositivi**: Desktop, tablet, smartphone
- **Connessione**: Funzionamento con connessioni moderate (3G+)
- **Offline**: Gestione disconnessioni temporanee

## 14. Casi d'Uso Principali

### 14.1 Registrazione e Accesso Master
1. Master si registra con email e password nel sistema
2. Accesso alla dashboard amministrativa con login completo
3. Creazione lega tramite interfaccia dedicata
4. Generazione magic link per ogni partecipante

### 14.2 Accesso Partecipanti tramite Magic Link
1. **Invito**: Master invia magic link univoco a ogni partecipante
2. **Accesso Diretto**: Partecipante clicca link per accesso immediato
3. **Identificazione**: Sistema riconosce partecipante tramite token nel link
4. **Sessione**: Accesso temporaneo limitato alla durata dell'asta

### 14.3 Creazione e Avvio Asta (Master)
1. Master crea lega tramite dashboard amministrativa
2. Configurazione parametri asta (budget, slot per ruolo, regole)
3. Generazione e invio magic link per partecipanti
4. Master monitora connessioni dalla dashboard di controllo
5. Avvio manuale dell'asta quando tutti sono presenti

### 14.4 Partecipazione - Interfaccia Unificata
1. **Accesso**: Partecipanti entrano tramite magic link nella pagina asta
2. **Esperienza Semplificata**: Dashboard con solo le funzioni necessarie
3. **Interazione**: Nominazione, offerte, visualizzazione stato
4. **Aggiornamenti**: Ricezione automatica eventi via SignalR

### 14.5 Gestione Master - Dashboard di Controllo
1. **Monitoraggio**: Visualizzazione completa stato asta e tutti i partecipanti
2. **Controllo**: Gestione eventi, pause, forzature turni
3. **Supervisione**: Controllo assegnazioni giocatori in tempo reale
4. **Intervento**: Risoluzione problemi e correzioni quando necessario

### 14.6 Ciclo Nominazione con Assegnazione Intelligente
1. **Turno Standard**:
   - Master visualizza nominatore corrente dalla dashboard
   - Utente nominatore seleziona giocatore dalla sua interfaccia
   - Sistema controlla slot disponibili per quel ruolo
2. **Assegnazione Automatica**:
   - Se solo nominatore ha slot → Assegnazione a 1 credito
   - Notifica SignalR immediata a tutti (utenti e master)
   - Master vede aggiornamento assegnazioni nella dashboard
3. **Fase Offerte** (se necessaria):
   - Ready-check partecipanti con slot disponibili
   - Master monitora timer e offerte dalla dashboard
   - Utenti partecipano tramite interfaccia semplificata

### 14.7 Completamento e Revisione Finale
1. **Rilevamento Automatico**: Sistema verifica completamento tutte le rose
2. **Anteprima Master**: Dashboard completa con tutte le squadre formate
3. **Anteprima Partecipanti**: Visualizzazione squadre nella pagina unificata
4. **Correzioni**: Master può intervenire tramite dashboard di controllo
5. **Conferma**: Approvazione da master e partecipanti

### 14.8 Gestione Database Giocatori e Svincoli Automatici
1. **Caricamento CSV**: Master carica file strutturato con dati aggiornati giocatori
2. **Elaborazione**: Sistema processa inserimenti, aggiornamenti e giocatori non più disponibili
3. **Svincoli Automatici**: 
   - Rilascio automatico giocatori che hanno lasciato il campionato
   - Rimborso al valore di mercato attuale (non prezzo acquisto)
   - Notifiche automatiche ai proprietari interessati
4. **Validazione**: Controllo conflitti e segnalazione problemi

### 14.9 Gestione Svincoli e Aste Riparazione
1. **Configurazione**: Master attiva periodo svincoli dalla dashboard
2. **Partecipazione**: Partecipanti gestiscono svincoli dalla loro interfaccia
3. **Asta Riparazione**: Stessa logica con interfacce dedicate per slot liberi
4. **Monitoraggio**: Master supervisiona tutto dalla dashboard di controllo
3. **Stessa Interfaccia**: Utilizzo sistema esistente con regole semplificate

## 15. Requisiti Tecnici Blazor e SignalR

### 15.1 Architettura Blazor SSR
- **Framework**: .NET 9 con Blazor Server-Side Rendering
- **Vantaggi**: Rendering server-side per performance ottimali
- **Componenti**: Struttura modulare e riutilizzabile
- **State Management**: Gestione stato centralizzata lato server

### 15.2 Comunicazione Real-time SignalR
- **Hub Dedicato**: Gestione connessioni specifiche per asta
- **Gruppi**: Organizzazione utenti per lega e sessione asta
- **Eventi Tipizzati**: Contratti definiti per ogni tipo di notifica
- **Resilienza**: Gestione riconnessioni automatiche

### 15.3 Metriche e Performance
- **Latenza SignalR**: Aggiornamenti sotto i 100ms
- **Concorrenza**: Supporto fino a 12 giocatori simultanei
- **Sessione Unica**: Una asta attiva per lega per garantire performance
- **Affidabilità**: Uptime 99.9% durante le sessioni attive
- **Scalabilità**: Supporto multiple leghe con aste indipendenti

### 15.4 Sicurezza e Integrità
- **Autenticazione**: Login sicuro per accesso sessioni
- **Autorizzazione**: Controlli granulari per azioni permesse
- **Validazione Server**: Tutte le operazioni validate lato server
- **Audit Trail**: Log completo per tracciabilità e debug

### 15.5 Gestione File CSV
- **Formato Supportato**: CSV con struttura predefinita per giocatori
- **Validazione**: Controllo integrità e formato file prima elaborazione
- **Backup Automatico**: Salvataggio stato precedente prima importazione
- **Elaborazione Batch**: Gestione efficiente grandi volumi di dati

## 16. Architettura Tecnica Dettagliata (DDD)

### 16.1 Principi Domain-Driven Design
- **Rich Domain Model**: Logica di business concentrata nelle entità
- **Self-Protecting Domain**: Entità che proteggono le proprie invarianti
- **Event-Driven Architecture**: Eventi di dominio per notifiche e sincronizzazione
- **Separation of Concerns**: Separazione netta tra domini, applicazione e infrastruttura

### 16.2 Struttura Progetti
```
Domain/
├── Entities/           # Entità aggregate root con logica business
├── ValueObjects/       # Oggetti valore immutabili
├── Services/          # Interfacce servizi di dominio
├── Events/            # Eventi di dominio
└── Specifications/    # Specifiche e regole business

Application/
├── Services/          # Servizi applicazione per orchestrazione
├── Interfaces/        # Contratti servizi applicazione
└── Notifications/     # Gestori eventi di dominio

Infrastructure/
├── Persistence/       # Implementazioni EF Core e interceptor
├── Services/          # Implementazioni servizi esterni
├── Notifications/     # SignalR e comunicazioni
└── Configuration/     # Setup e configurazioni

Portal/ (Blazor SSR)
├── Pages/            # Pagine Blazor
├── Components/       # Componenti riutilizzabili
├── Services/         # Servizi UI-specific
└── Models/           # View models se necessari
```

### 16.3 Pattern Entità Protette
```csharp
// Esempio di entità self-protecting
public class AuctionTurn
{
    // Costruttore privato per EF Core
    private AuctionTurn() { }
    
    // Factory method controllata
    public static async Task<AuctionTurn> CreateAsync(
        Guid auctionId, 
        Guid nominatorId,
        IPlayerAvailabilityService playerService,
        CancellationToken ct)
    {
        // Validazioni e logica di creazione
        var turn = new AuctionTurn();
        // ... inizializzazione protetta
        return turn;
    }
    
    // Metodi business protetti
    public async Task<PlaceBidResult> PlaceBidAsync(
        Bid bid, 
        IBudgetValidationService budgetService,
        CancellationToken ct)
    {
        // Validazioni invarianti
        // Logica business
        // Emissione eventi dominio
    }
    
    // Proprietà protette
    public BidCollection Bids { get; private set; }
    public TurnStatus Status { get; private set; }
}
```

### 16.4 Gestione Concorrenza e Sincronizzazione
- **Semafori per Bid**: Lock pessimistico su offerte simultanee
- **Primi Arrivati Serviti**: Risoluzione automatica conflitti temporali
- **Eventi Ordinati**: Sequenza garantita tramite eventi di dominio
- **Transazioni Database**: Ogni comando persistito atomicamente

### 16.5 Blazor SSR + EF Core Pattern
```csharp
// Componente Blazor con pattern diretto
@inject IDbContextFactory<AuctionDbContext> DbFactory
@inject IAuctionService AuctionService

@code {
    private async Task PlaceBidAsync()
    {
        using var db = DbFactory.CreateDbContext();
        
        var turn = await db.AuctionTurns
            .FirstAsync(t => t.Id == CurrentTurnId);
            
        var result = await turn.PlaceBidAsync(
            new Bid(UserId, BidAmount),
            AuctionService,
            CancellationToken.None);
        
        await db.SaveChangesAsync();
        
        // Eventi di dominio automaticamente 
        // propagati via EF Core interceptor
    }
}
```

### 16.6 Validazione UI con DataAnnotations
```csharp
// Entità con validazioni integrate
public class Bid
{
    [Required(ErrorMessage = "L'importo è obbligatorio")]
    [Range(1, int.MaxValue, ErrorMessage = "L'importo deve essere positivo")]
    public int Amount { get; private set; }
    
    [Required]
    public Guid PlayerId { get; private set; }
}

// Form Blazor automatico
<EditForm Model="@bid" OnValidSubmit="@PlaceBidAsync">
    <DataAnnotationsValidator />
    <ValidationSummary />
    
    <InputNumber @bind-Value="bid.Amount" />
    <ValidationMessage For="@(() => bid.Amount)" />
    
    <button type="submit">Fai Offerta</button>
</EditForm>
```

### 16.7 Event-Driven Notifications
```csharp
// Evento di dominio
public record BidPlacedEvent(AuctionTurn Turn, Bid Bid) : IDomainEvent;

// Handler per SignalR
public class BidPlacedNotificationHandler : INotificationHandler<BidPlacedEvent>
{
    public async Task Handle(BidPlacedEvent notification, CancellationToken ct)
    {
        await _signalRService.NotifyBidPlaced(
            auctionId: notification.Turn.AuctionId,
            bidderName: notification.Bid.PlayerName,
            amount: notification.Bid.Amount
        );
    }
}
```

### 16.8 Mobile UI Ottimizzata
- **Layout Semplificato**: Focus su azioni essenziali
- **Componenti Principali**:
  - Status giocatore corrente in puntata
  - Ready/waiting status
  - Pulsanti offerta (+1, +5, +10, custom)
  - Log ultime offerte
  - Rosa personale compatta
- **Dialog Secondari**:
  - Squadre altri partecipanti
  - Configurazioni asta
  - Log eventi completo
  - Statistiche dettagliate

### 16.9 Gestione Resilienza
- **Persistenza Completa**: Nessuno stato in memory critico
- **Recovery Automatico**: Ricostruzione stato da eventi persistiti
- **Disconnessioni**: Gestione timeout con controllo master
- **Backup Continuo**: Ogni transazione salvata immediatamente
- **Monitoring**: Log completo per diagnostica e audit

### 16.10 Testing Strategy
```csharp
// Unit Test Dominio
[Test]
public async Task PlaceBid_WhenValidAmount_ShouldSucceed()
{
    // Arrange
    var budgetService = Mock.Of<IBudgetValidationService>();
    var turn = await AuctionTurn.CreateAsync(...);
    
    // Act
    var result = await turn.PlaceBidAsync(bid, budgetService, CancellationToken.None);
    
    // Assert
    Assert.That(result.IsSuccess);
    Assert.That(turn.CurrentHighestBid.Amount, Is.EqualTo(bidAmount));
}

// Integration Test
[Test]
public async Task PlaceBidCommand_ShouldPersistAndNotify()
{
    // Test completo con database e SignalR
}
```

## 17. Roadmap di Sviluppo

### 17.1 Fase 1 - Fondamenta (4-6 settimane)
**Obiettivo**: Setup architetturale e funzionalità core

#### **Sprint 1-2: Infrastructure & Domain**
- ✅ Setup progetti DDD (Domain, Application, Infrastructure, Portal)
- ✅ Configurazione EF Core con DbContextFactory
- ✅ Implementazione entità core (League, AuctionEvent, AuctionTurn, Bid)
- ✅ Value Objects (PlayerId, Budget, BidAmount)
- ✅ Domain Services interfaces
- ✅ Event-driven architecture con EF Core interceptor

#### **Sprint 3: Authentication & Basic UI**
- ✅ Sistema autenticazione Master (email/password)
- ✅ Magic link per partecipanti
- ✅ Dashboard Master base
- ✅ Pagina partecipanti base
- ✅ Setup SignalR Hub

#### **Deliverable Fase 1**
- Architettura completa funzionante
- Autenticazione differenziata
- Entità domain con invarianti
- Setup base UI Blazor SSR

### 17.2 Fase 2 - Core Auction (6-8 settimane)
**Obiettivo**: Meccanica asta completa

#### **Sprint 4-5: Auction Logic**
- ✅ Creazione e configurazione leghe
- ✅ Caricamento giocatori da CSV
- ✅ Logica turni e nominazioni
- ✅ Sistema di assegnazione automatica
- ✅ Gestione slot per ruolo

#### **Sprint 6-7: Bidding System**
- ✅ Meccanica offerte con semafori
- ✅ Timer e countdown
- ✅ Ready-check partecipanti
- ✅ Validazioni budget real-time
- ✅ Eventi SignalR per sincronizzazione

#### **Sprint 8: UI Auction Room**
- ✅ Dashboard Master completa
- ✅ Interfaccia partecipanti ottimizzata
- ✅ Mobile UI semplificata
- ✅ Real-time updates via SignalR

#### **Deliverable Fase 2**
- Asta completa funzionante
- UI responsive per tutte le piattaforme
- Sincronizzazione real-time
- Gestione completa turni e offerte

### 17.3 Fase 3 - Advanced Features (4-5 settimane)
**Obiettivo**: Funzionalità avanzate e management

#### **Sprint 9-10: Player Management**
- ✅ Sistema svincoli automatici da CSV
- ✅ Gestione giocatori non disponibili
- ✅ Svincoli manuali con regole
- ✅ Aste di riparazione

#### **Sprint 11: Configuration & Admin**
- ✅ Configurazioni dinamiche asta
- ✅ Validazioni configurazioni (slot minimi)
- ✅ Controlli Master avanzati
- ✅ Pause/resume asta

#### **Sprint 12: Polish & UX**
- ✅ Ottimizzazioni performance
- ✅ Error handling robusto
- ✅ UI/UX refinement
- ✅ Validazioni complete

#### **Deliverable Fase 3**
- Sistema completo con tutte le funzionalità
- Gestione avanzata configurazioni
- Esperienza utente ottimizzata

### 17.4 Fase 4 - Testing & Production (3-4 settimane)
**Obiettivo**: Stabilizzazione e deploy

#### **Sprint 13: Testing Completo**
- ✅ Unit tests Domain entities
- ✅ Integration tests con database
- ✅ End-to-end tests asta completa
- ✅ Load testing con 12 utenti simultanei
- ✅ SignalR stress testing

#### **Sprint 14: Production Ready**
- ✅ Logging e monitoring
- ✅ Performance optimization
- ✅ Security hardening
- ✅ Deployment pipeline

#### **Sprint 15: Go Live**
- ✅ Deploy produzione
- ✅ Monitoring setup
- ✅ Documentation utente
- ✅ Support e maintenance

#### **Deliverable Fase 4**
- Sistema in produzione
- Test coverage completo
- Monitoring e alerting
- Documentazione completa

### 17.5 Milestone e Criteri di Successo

#### **Milestone 1 (Fine Fase 1)**
- [ ] Architettura DDD funzionante
- [ ] Autenticazione completa
- [ ] Setup base UI

#### **Milestone 2 (Fine Fase 2)**
- [ ] Asta completa end-to-end
- [ ] 12 utenti simultanei
- [ ] Mobile-friendly UI

#### **Milestone 3 (Fine Fase 3)**
- [ ] Tutte le funzionalità implementate
- [ ] Configurazioni dinamiche
- [ ] UX ottimizzata

#### **Milestone 4 (Fine Fase 4)**
- [ ] Sistema in produzione
- [ ] Performance target raggiunti
- [ ] Zero critical bugs

### 17.6 Rischi e Mitigazioni

#### **Rischi Tecnici**
- **SignalR Scalability**: Test early con 12+ utenti
- **EF Core Performance**: Ottimizzazione query e indexing
- **Concurrency Issues**: Test stress su bid simultanee

#### **Rischi di Scope**
- **Feature Creep**: Stick alle specifiche definite
- **UI Complexity**: Mantenere mobile-first approach
- **Over-Engineering**: YAGNI principle

#### **Mitigazioni**
- Weekly review con stakeholder
- Performance testing ogni sprint
- Continuous integration/deployment
- Regular architecture reviews

### 17.7 Team e Responsabilità

#### **Roles Suggeriti**
- **Tech Lead**: Architettura e code review
- **Backend Developer**: Domain logic e Infrastructure
- **Frontend Developer**: Blazor UI e SignalR
- **QA Engineer**: Testing strategy e automation

#### **Definition of Done**
- [ ] Code review completato
- [ ] Unit tests con coverage > 80%
- [ ] Integration tests passanti
- [ ] UI responsiva testata
- [ ] Performance baseline mantenuta

---

*Questo documento definisce le specifiche funzionali, tecniche e la roadmap completa per il sistema di aste fantacalcio. L'architettura DDD garantisce robustezza, testabilità e manutenibilità, mentre l'approccio event-driven assicura sincronizzazione real-time e resilienza del sistema.*

---

## Appendice A — Struttura soluzione (progetti e cartelle)

```
FantaAsta.sln
Domain/            # Entità, servizi di dominio, eventi, eccezioni, enums
Application/       # Timer, orchestrazione eventi applicativi, publisher eventi
Infrastructure/    # EF Core DbContext, configurazioni, hosted services, validator concreti
Portal/            # Blazor Server (UI), SignalR Hub e servizi client
Tests/             # Unit e integration tests
```

## Appendice B — Allineamento richieste DDD → implementazione (riferimenti file)

- Enums nel dominio: `Domain/Enums/*`
- Costruttori privati e factory statiche: es. `Domain/Entities/SerieAPlayer.cs` (metodo `Create`), `Domain/Entities/Team.cs`, `Domain/Entities/AuctionSession.cs`
- Validator via DI: `Domain/Contracts/ITeamValidator.cs` con implementazioni in `Infrastructure/Validators/*`
- EF Core fuori dal dominio: `Infrastructure/Peristance/*`, configurazioni in `Infrastructure/Peristance/Configuration/*`, DbContext `ApplicationDbContext`
- Eventi di dominio e dispatch post‑save: emissione in Domain, dispatch in `Infrastructure/Peristance/ApplicationDbContext.cs`
- Timer e orchestrazione applicativa: `Application/Services/TimerWorker.cs`, `Application/Services/AuctionTimerManager.cs`, `Application/Services/AuctionTimerHostedService.cs`, eventi applicativi in `Application/Events/*`
- Finalizzazione scadenze/assegnazioni: `Infrastructure/Services/AuctionFinalizationHostedService.cs`
- Realtime: `Portal/Hubs/AuctionHub.cs`, `Portal/Services/SignalRRealtimeNotificationService.cs`, `Portal/Services/AuctionHubClient.cs`
- Test: `Tests/*.cs` (AuctionSession*, LeagueTests, ecc.)

## Appendice C — SignalR: endpoint e contratti eventi

- Hub: percorso `/hubs/auction` esposto da `AuctionHub` (file `Portal/Hubs/AuctionHub.cs`)
- Gruppi: `league:{leagueId}`, `auction:{auctionId}`, `turn:{turnId}`
- Eventi inviati dal server: `TimerUpdate`, `TimerWarning` con payload `{ leagueId, auctionId, turnId, remainingSeconds }`
- Servizio server per broadcast: `Portal/Services/SignalRRealtimeNotificationService.cs`
- Client helper Blazor: `Portal/Services/AuctionHubClient.cs` (metodi `StartAsync`, `Join*`, eventi `TimerUpdate`/`TimerWarning`)

## Appendice D — Build, test e avvio locale

Esegui dalla radice del repository (Windows, cmd):

```cmd
dotnet restore FantaAsta.sln
dotnet build FantaAsta.sln
dotnet test --no-build --logger "trx;LogFileName=test-results.trx"
```

Avvio del Portal (Blazor Server):

```cmd
dotnet run --project Portal/Portal.csproj
```

L'hub SignalR è disponibile su `/hubs/auction`.

## Appendice E — Magic link e pagine (sintesi operativa)

- Ruoli: Master (login completo), Partecipante/Ospite (magic link con grant limitato)
- Pattern URL proposti:
  - Partecipante: `/join/{token}` → grant per (leagueId, sessionId, teamId), redirect a `/app/partecipante/{leagueId}/{sessionId}`
  - Ospite: `/watch/{token}` → grant read‑only, redirect a `/app/ospite/{leagueId}/{sessionId}`
  - Admin (opzionale): `/adminlink/{token}` → grant ruolo Master limitato alla lega
- Pagine richieste:
  - Master Dashboard: elenco leghe, settings, creazione sessione unica, admin sessione (forza/reset/modify, pausa/riprendi, log eventi)
  - Partecipante (mobile‑first): team, giocatore attivo, ready/azioni, offerte rapide, roster altri/crediti, timer realtime, immagini `SerieAPlayer.ImageUrl`
  - Ospite (desktop): dashboard read‑only della sessione corrente

## Appendice F — Stato attuale sintetico

- Dominio e orchestrazione asta (readiness, offerte con min‑incremento, timer, scadenze) presenti
- Realtime server/client presente (SignalR Hub + servizio + client Blazor ausiliario)
- UI finale (Master/Partecipante/Ospite) e magic link: da implementare
- Outbox affidabile e persistenza mapping timer/turno: da implementare
