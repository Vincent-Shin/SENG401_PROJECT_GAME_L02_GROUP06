# Deployment

- `firebase.json`: current frontend hosting config used by the live deployment.
- `firebase.test.json`: separate frontend hosting config for a test deployment.
- `render.yaml`: original Render blueprint used earlier in the project.
- `render.test.yaml`: separate Render blueprint for a test backend service.
- `.firebaserc.example`: example Firebase alias mapping for keeping production and test sites separate.

Use the test files only from a non-`main` branch so the current live links stay unchanged.
