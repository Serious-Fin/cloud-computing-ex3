# Frontend

Plain HTML/CSS/JavaScript page (no build step, no frameworks) that uses the `api` project.

## Run it

1. Start the API (from the `api` folder):

   ```
   dotnet run --launch-profile http
   ```

   It listens on `http://localhost:5016`.

2. Open `index.html` in a browser. Any of these works:
   - Double-click `frontend/index.html` (opens as a `file://` page).
   - Or serve the folder with any static file server, e.g. VS Code's Live Server extension.

3. If the API runs on a different address, change `API_BASE` at the top of `app.js`.

## What it does

| Action | HTTP call |
| --- | --- |
| List tires | `GET /tires` |
| Create tire | `POST /tires` |
| Update tire | `PUT /tires/{id}` |
| Delete tire | `DELETE /tires/{id}` |

Validation errors returned by the API are shown under the form. The API also allows
cross-origin requests (see `AddCors` in `api/Program.cs`), so the page can be hosted
separately from the API.
