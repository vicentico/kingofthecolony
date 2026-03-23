# Diagrama del Modelo de Datos

## Objetivo

Este documento contiene una versión separada y reutilizable del diagrama Mermaid del modelo de datos.

Está pensado para:

1. arquitectura;
2. documentación ejecutiva;
3. presentaciones;
4. revisión rápida de relaciones sin leer el detalle completo del modelo.

Para la explicación completa de cada entidad, ver [DATA-MODEL.md](DATA-MODEL.md).

---

## Diagrama ER

```mermaid
erDiagram
    User {
        int Id PK
        string GoogleId UK
        string PasswordHash
        string Email UK
        string DisplayName
        string AvatarUrl
        int Credits
        int Wins
        int Losses
        int BestStreak
        int CurrentStreak
        datetime CreatedAt
    }

    GameRoom {
        int Id PK
        string Name
        int KingUserId FK
        int ChallengerUserId FK
        string Status
        string HostIp
        int UdpPort
        string GameRom
        datetime CreatedAt
    }

    QueueEntry {
        int Id PK
        int GameRoomId FK
        int UserId FK
        int Position
        datetime JoinedAt
    }

    Spectator {
        int Id PK
        int GameRoomId FK
        int UserId FK
        datetime JoinedAt
    }

    MatchHistory {
        int Id PK
        int GameRoomId
        int WinnerId FK
        int LoserId FK
        datetime PlayedAt
    }

    MatchSession {
        guid Id PK
        int GameRoomId FK
        int KingUserId FK
        int ChallengerUserId FK
        int WinnerUserId FK
        int LoserUserId FK
        string Status
        string GameRom
        string ResultSource
        string LaunchSource
        string ClientInstanceId
        string EvidencePayload
        string ResultReason
        string LastIdempotencyKey
        int EmulatorProcessId
        int CreatedByUserId FK
        int ReportedByUserId FK
        datetime CreatedAtUtc
        datetime StartedAtUtc
        datetime EndedAtUtc
        datetime ReportedAtUtc
    }

    CreditTransaction {
        int Id PK
        int UserId FK
        int Amount
        string Type
        string Description
        datetime CreatedAt
    }

    User ||--o{ QueueEntry : joins
    User ||--o{ Spectator : watches
    User ||--o{ CreditTransaction : owns
    User ||--o{ MatchHistory : wins_or_loses
    User ||--o{ MatchSession : participates

    GameRoom ||--o{ QueueEntry : contains
    GameRoom ||--o{ Spectator : contains
    GameRoom ||--o{ MatchSession : tracks

    GameRoom }o--|| User : king
    GameRoom }o--|| User : challenger

    QueueEntry }o--|| User : queued_user
    QueueEntry }o--|| GameRoom : room

    Spectator }o--|| User : spectator_user
    Spectator }o--|| GameRoom : room

    MatchSession }o--|| GameRoom : room
    MatchSession }o--|| User : king
    MatchSession }o--|| User : challenger
    MatchSession }o--|| User : winner
    MatchSession }o--|| User : loser
    MatchSession }o--|| User : created_by
    MatchSession }o--|| User : reported_by

    MatchHistory }o--|| User : winner
    MatchHistory }o--|| User : loser

    CreditTransaction }o--|| User : owner
```

---

## Lectura rápida del diagrama

- `User` es la entidad central del sistema.
- `GameRoom` modela la sala activa con un `KingUserId` y un `ChallengerUserId` opcionales.
- `QueueEntry` y `Spectator` representan la vista viva de usuarios esperando o mirando.
- `MatchSession` modela la operación de una partida concreta y sirve para tracking, overlay y cierre manual.
- `MatchHistory` guarda solo resultados oficiales que impactan ranking.
- `CreditTransaction` funciona como ledger de créditos.

---

## Notas de uso

- Si necesitas contexto funcional, usa [MATCH-FLOW-AND-OVERLAY-DESIGN.md](MATCH-FLOW-AND-OVERLAY-DESIGN.md).
- Si necesitas detalle campo por campo y restricciones, usa [DATA-MODEL.md](DATA-MODEL.md).
