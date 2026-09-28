# Group Assignment Manager

A demo app for university group assignments, built with ASP.NET Core 10, React + TypeScript, and MongoDB.

## Run locally

Prerequisites: .NET 10 SDK, Node 20.19+ or 22.12+, and Docker Desktop (or a running MongoDB server).

1. From the repository root, run `docker compose up -d` to start MongoDB.
2. In one terminal, run `cd backend` then `dotnet restore` and `dotnet run --launch-profile http`. The API listens on http://localhost:5004.
3. In another terminal, run `cd frontend` then `npm install` and `npm run dev`. Open http://localhost:5173.
4. Create users for your team, select a user, create a project, and add the others as members. The data survives backend restarts.

For MongoDB Atlas, set the environment variable `MongoDb__ConnectionString` in the backend terminal. Never commit credentials. The Vite development server proxies /api to port 5004.

## Demo walkthrough

Create Eden and Maya, make a project as Eden, add Maya. Create an unassigned task. Switch to Maya and request it. Switch to Eden and assign Maya. Maya can mark it completed or flag it as needing help. To show conflict handling, open the same board in two browser windows and load the same task. Update it in the first window; saving the older version in the second returns HTTP 409, refreshes the board, and shows an explanation. Use two separate browser profiles or tabs and choose different demo users if desired.

## Architecture and rules

- Controllers map HTTP requests to services and repositories. Repositories read and write three MongoDB collections: `users`, `projects`, and `tasks`.
- A project creator is automatically a member. A task belongs to one existing project. Assignment and assignment requests require project membership. An unassigned task must be requested before assignment; an assigned task may be transferred to another member.
- All task mutations include the version last seen by the browser. MongoDB atomically matches the task ID, version, and any relevant assignment conditions and increments the version in the same operation. A failed match returns 409 if the task still exists. This prevents one task update from silently overwriting another. HTTP 409 asks the user to review fresh data; it does not merge edits.
- Switching demo user is **not authentication or authorization**. There is no login, and anyone who can call the API can select any user ID. This is a learning demo, not a production access control design.
- Cross-collection rules (such as membership removal racing task assignment) are not transactional. Production would require authorization, indexes for unique email, database transactions or a stricter membership design, and pagination.

## API highlights

`GET/POST /api/users`, `GET/POST /api/projects`, `POST /api/projects/{id}/members`, `GET/POST /api/tasks?projectId={guid}`, `PUT /api/tasks/{id}`, `POST /api/tasks/{id}/request-assignment`, `PUT /api/tasks/{id}/assign`, `PUT /api/tasks/{id}/unassign`, `PUT /api/tasks/{id}/status`, `PUT /api/tasks/{id}/help`, and `DELETE /api/tasks/{id}?version=N`.

For each mutation, pass `version` in JSON, e.g. `{"version":1,"needsHelp":true}`. Responses include the new version. A stale version returns 409. OpenAPI JSON is exposed at `/openapi/v1.json` in Development.
