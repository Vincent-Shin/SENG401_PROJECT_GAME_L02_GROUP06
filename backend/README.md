# Backend Database Setup

## What is included
- `backend.py` now initializes SQLAlchemy and creates core tables (`players`, `applications`) for local development.
- `schema.sql` defines the full game database schema from current code + UML/planned systems.

## Quick local run (SQLite)
1. Install dependencies:
   - `pip install flask flask-cors flask-sqlalchemy sqlalchemy`
2. Run:
   - `python backend.py`
3. The file `unemployed_simulator.db` is created automatically in `backend/`.

## Use PostgreSQL or MySQL
1. Set `DATABASE_URL` before running backend:
   - PostgreSQL example: `export DATABASE_URL='postgresql+psycopg://user:pass@localhost:5432/unemployed_sim'`
   - MySQL example: `export DATABASE_URL='mysql+pymysql://user:pass@localhost:3306/unemployed_sim'`
2. Apply the full schema:
   - `schema.sql` (execute with your DB client)
3. Start API:
   - `python backend.py`

## Notes
- Existing API routes still work:
  - `GET /player/<id>`
  - `POST /player/update-score`
  - `POST /apply`
  - `GET /applications`
- The full schema includes planned tables for resume scoring, projects, market phases, interviews, and job offers.
