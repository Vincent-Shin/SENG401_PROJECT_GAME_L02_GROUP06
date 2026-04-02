# Isolated Test Environment

This branch is intended for testing WebGL/UI changes without touching the live links currently used by the team.

## Goal

Keep the current production deployment unchanged while testing:

- intro video fixes
- WebGL canvas sizing / fullscreen fixes
- pointer lock fixes
- login flow fixes

## Production Links To Leave Untouched

- Frontend: `https://unemployed-simulator-web.web.app/`
- Backend: `https://seng401-project-game-l02-group06.onrender.com`

Do not redeploy `main` while validating test-only changes.

## Recommended Test Stack

- Git branch: `test/isolated-webgl-deploy`
- Firebase Hosting project: create a separate test project/site
- Render backend service: create a separate free web service
- Supabase: create a separate test project if you want fully isolated data

## Render Test Backend

Use `render.test.yaml` or create a manual web service with:

- Service name: `unemployed-simulator-backend-test`
- Root directory: `backend`
- Build command: `pip install -r requirements.txt`
- Start command: `gunicorn -w 2 -b 0.0.0.0:$PORT backend:app`
- Health check path: `/health`

Set `DATABASE_URL` to a separate Supabase test project if you want to isolate test data.

## Firebase Test Frontend

1. Create a separate Firebase project/site for test only.
2. Copy `.firebaserc.example` to `.firebaserc` locally.
3. Replace `your-test-firebase-project-id` with the new Firebase test project id.
4. Deploy the test site using `firebase.test.json`.

Example:

```powershell
firebase use test
firebase deploy --only hosting --config firebase.test.json
```

## Unity URLs To Update On The Test Branch

After the new Render backend is live, update the test branch only:

- `Assets/Scenes/ResumeLogic.cs`
- `Assets/Scenes/Top5LeaderboardUI.cs`
- `Assets/Image/Free Japanese City Game Assets/Demo/IntroScene.unity`

Replace the production backend URL with the test backend URL.

## Safe Workflow

1. Work only on the test branch.
2. Deploy the test backend.
3. Point the test branch Unity client to the test backend URL.
4. Build WebGL from the test branch.
5. Deploy frontend to the test Firebase site.
6. Verify changes there first.
7. Only merge to `main` after the test deployment is confirmed stable.
