# SENG401_PROJECT_GAME_L02_GROUP06

## Unemployed Simulator

Serious game project for SENG 401.

The game is a 2D Unity experience about building a resume, completing minigames, surviving hiring pressure, and progressing through company tiers from startup to big tech.

## Group Members

- Mai, Trung Tuan
- Ndabaramiye, Benny
- Sekhon, Manjot
- Vuong, Justin

## SDG Focus

- United Nations Sustainable Development Goal 8
- Decent Work and Economic Growth

## Live Links

- Game deployment: `https://unemployed-simulator-web-test.web.app`
- Backend API: `https://seng401-project-game-l02-group06-test.onrender.com`
- Backend health check: `https://seng401-project-game-l02-group06-test.onrender.com/health`
- Repository: `https://github.com/Vincent-Shin/SENG401_PROJECT_GAME_L02_GROUP06.git`

## Main Project Areas

- `Assets/`
  Unity scenes, UI, gameplay logic, and minigames
- `backend/`
  Flask API, persistence logic, SQL schema, and deploy settings
- `documentation/`
  Architecture, testing artifacts, design notes, and presentation-facing summaries
- `ProjectSettings/` and `Packages/`
  Unity project configuration

## Layered Architecture

### Presentation Layer

- Unity WebGL / Unity 2D client
- Player movement
- UI panels
- Minigames
- Leaderboard display

### Application / Business Layer

- Unity gameplay logic
  Resume progression, market phase simulation, stock trading logic, and minigame result flow
- Flask backend API
  Player login/load, score updates, company application logic, and leaderboard logic

### Data Layer

- PostgreSQL on Supabase for deployed runtime
- SQLite fallback for local development
- SQLAlchemy ORM for persistence

## Deployment Stack

- Frontend: Firebase Hosting
- Backend: Render
- Database: Supabase PostgreSQL

## Minigames

- Resume activity
- Resume tailoring / swipe
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
