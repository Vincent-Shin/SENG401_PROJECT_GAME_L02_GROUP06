# Layered Architecture

## Presentation Layer

- Unity WebGL / Unity 2D client
- Player movement and scene interaction
- UI panels
- Minigames
- Leaderboard display
- HTTP requests via `UnityWebRequest`

Key files:
- `Assets/Scenes/PlayerController.cs`
- `Assets/Scenes/AccountManager.cs`
- `Assets/Scenes/ResumeLogic.cs`
- `Assets/Scenes/Companypanel.cs`
- `Assets/Scenes/Top5LeaderboardUI.cs`

## Application / Business Layer

### Unity gameplay logic
- Resume progression
- Market phase simulation
- Minigame rules and win/loss flow
- Stock trading minigame logic

Key files:
- `Assets/Scenes/MarketPhaseController.cs`
- `Assets/Scenes/CandlestickSpawnTest.cs`
- `Assets/Scenes/ResumeActivityInteraction.cs`
- `Assets/Scenes/ProjectMinigameSceneController.cs`
- `Assets/Scenes/NetworkingMemoryMinigameInteraction.cs`
- `Assets/Scenes/CertificateMinigameInteraction.cs`
- `Assets/Scenes/ResumeSwipeMinigameInteraction.cs`
- `Assets/Scenes/ResumeTailoredMinigameInteraction.cs`

### Backend service logic
- Flask REST API
- Player load/create
- Resume score updates
- Company application processing
- Requirement validation
- Leaderboard generation

Key file:
- `backend/backend.py`

## Data Layer

- PostgreSQL on Supabase for deployed runtime
- SQLite fallback for local development
- Player and application persistence via SQLAlchemy ORM

Main persisted entities:
- Players
- Applications

Key files:
- `backend/backend.py`
- `backend/schema.sql`

## Deployment Layer

- Frontend hosting: Firebase Hosting
- Backend hosting: Render
- Database hosting: Supabase PostgreSQL
