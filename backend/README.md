## Backend quick deploy notes

This Flask API is the public backend for the Unity game.

### What the current runtime actually uses
- `backend.py` is the source of truth for the live database model.
- The game currently depends on the `players` and `applications` tables created from the SQLAlchemy models in `backend.py`.
- `schema.sql` contains planned tables too, but it is not fully aligned with the runtime model yet.

### Important deployment warning
- For a fresh public deployment, start with an empty database and let `backend.py` create the required runtime tables automatically.
- Do **not** pre-apply `schema.sql` unless you update it to match the current SQLAlchemy models first.

### Local run
1. Create a virtual environment.
2. Install dependencies:
   - `pip install -r requirements.txt`
3. Run:
   - `python backend.py`

### Public deployment
Recommended fast path:
- Host the Flask API on a Python web service.
- Use PostgreSQL for the database.
- Set `DATABASE_URL` in the host environment.
- Start command:
  - `gunicorn -w 2 -b 0.0.0.0:$PORT backend:app`

If your host uses the `backend/` folder as the service root, the command above works directly.

### Required environment
- `DATABASE_URL`
  - Example PostgreSQL SQLAlchemy URL:
  - `postgresql+psycopg://USER:PASSWORD@HOST:5432/DBNAME`

### Current gameplay rules in backend
- Default starting resume score: `42`
- `startup`
  - minimum score `50`
  - reward `+6`
  - requires `resume_activity`
- `mid_tier`
  - minimum score `70`
  - reward `+10`
  - requires `project`
  - requires successful `startup`
- `big_tech`
  - minimum score `85`
  - reward `+0`
  - requires `life_experience` and `certificate`
  - requires successful `mid_tier`

### Leaderboard behavior
- Only players who successfully win `big_tech` appear.
- Ranking sorts by:
  - higher score first
  - then lower completion time

### Current API routes
- `GET /health`
- `GET /player/<id>`
- `POST /player/load-or-create`
- `POST /player/update-score`
- `POST /player/update-stage`
- `POST /player/add-bonus`
- `POST /player/complete-activity`
- `POST /player/reset-run`
- `POST /player/delete`
- `POST /apply`
- `GET /applications`
- `GET /leaderboard/top3`
