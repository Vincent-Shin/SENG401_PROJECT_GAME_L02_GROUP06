# SENG401_PROJECT_GAME_L02_GROUP06

## Unemployed Simulator

Serious game project for SENG 401.

The game is a 2D Unity experience about trying to build a resume, complete minigames, survive hiring pressure, and progress through company tiers from startup to big tech.

## SDG Focus

- United Nations Sustainable Development Goal 8
- Decent Work and Economic Growth

## Main Project Areas

- `Assets/`
  - Unity scenes, UI, gameplay logic, and minigames
- `backend/`
  - Flask API, persistence logic, SQL schema, and deploy settings
- `documentation/`
  - architecture, testing artifacts, design notes, and presentation-facing summaries
- `ProjectSettings/` and `Packages/`
  - Unity project configuration

## Layered Architecture

### Presentation Layer
- Unity WebGL / Unity 2D client
- Player movement
- UI panels
- Minigames
- Leaderboard display

### Application / Business Layer
- Unity gameplay logic
  - Resume progression
  - Market phase simulation
  - Stock trading logic
  - Minigame rules and result flow
- Flask backend API
  - Player login/load
  - Score updates
  - Company application logic
  - Leaderboard logic

### Data Layer
- PostgreSQL on Supabase for deployed runtime
- SQLite fallback for local development
- SQLAlchemy ORM for persistence

## Deployment

- Frontend: Firebase Hosting
- Backend: Render
- Database: Supabase PostgreSQL

## Minigames

- Resume activity
- Resume tailoring
- Certificate
- Networking
- Project
- Life experience / stock trading

## Documentation

See:
- `documentation/architecture/`
- `documentation/design-artifacts/`
- `documentation/testing-artifacts/`
- `documentation/presentation/`
