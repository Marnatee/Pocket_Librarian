## Version 4

- Added a backend-only `GET /api/health` endpoint so the dashboard can show the local ASP.NET backend as Online independently of SQL Server.
- Added a separate `GET /api/pipeline/database-status` endpoint for the database card.
- Limited database connection and command probes to three seconds so failed SQL checks return quickly.
- Added a five-second client timeout to the API pipeline demonstration.

This file explains how Visual Studio created the project.

The following steps were used to generate this project:
- Create new ASP\.NET Core Web API project.
- Update project file to add a reference to the frontend project and set SPA properties.
- Update `launchSettings.json` to register the SPA proxy as a startup assembly.
- Add project to the startup projects list.
- Write this file.
