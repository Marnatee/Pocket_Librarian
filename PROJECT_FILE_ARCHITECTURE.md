# Pocket Librarian project-file architecture listing

This inventory covers every file included in this version of the project. The requested category names are used exactly: `Frontend`, `BLL`, `DAL`, and `Documentation`.

## Architecture overview

| Requested category | N-layer role | N-tier role |
| --- | --- | --- |
| Frontend | Presentation layer | Client/presentation tier |
| BLL | API, application, and business-logic layer | ASP.NET Core server tier |
| DAL | Data-access layer | Database-access portion of the server tier |
| Documentation | Supporting project material and configuration; not a runtime layer | Supporting project material; not a runtime tier |

The runtime flow is: **React frontend → ASP.NET Core controller → business service → data-access repository → SQL Server**. The controller, service, and repository are intentionally kept separate to demonstrate the three-layer pipeline required by the project.

## Frontend

| File | N-layer / N-tier role |
| --- | --- |
| `pocket_librarian.client/.gitignore` | Frontend project housekeeping; client-side project support |
| `pocket_librarian.client/.vscode/launch.json` | Frontend development launch configuration; client-side project support |
| `pocket_librarian.client/eslint.config.js` | Frontend linting configuration; client-side project support |
| `pocket_librarian.client/index.html` | Frontend document entry point; presentation layer / client tier |
| `pocket_librarian.client/package.json` | Frontend dependency and script manifest; client-side project support |
| `pocket_librarian.client/package-lock.json` | Frontend dependency lockfile; client-side project support |
| `pocket_librarian.client/pocket_librarian.client.esproj` | Frontend project definition; client-side project support |
| `pocket_librarian.client/vite.config.js` | Frontend development-server and API-proxy configuration; presentation/client tier support |
| `pocket_librarian.client/src/main.jsx` | React bootstrap entry point; presentation layer / client tier |
| `pocket_librarian.client/src/App.jsx` | Application shell, page switching, and health-check orchestration; presentation layer / client tier |
| `pocket_librarian.client/src/App.css` | Shared application styling; presentation layer / client tier |
| `pocket_librarian.client/src/index.css` | Global frontend styling and reset; presentation layer / client tier |
| `pocket_librarian.client/src/components/AppBanner.jsx` | Persistent banner, page navigation, and intentionally empty login buttons; presentation layer / client tier |
| `pocket_librarian.client/src/components/ConnectionStatusDashboard.jsx` | Frontend status dashboard and masked detail toggles; presentation layer / client tier |
| `pocket_librarian.client/src/components/FeaturePlaceholder.jsx` | Reusable under-construction feature card; presentation layer / client tier |
| `pocket_librarian.client/src/components/FeaturedBookCarousel.jsx` | Restored rotating Featured Book demonstration; presentation layer / client tier |
| `pocket_librarian.client/src/components/PipelineTest.jsx` | Restored three-layer API pipeline demonstration; presentation layer / client tier |
| `pocket_librarian.client/src/pages/HomePage.jsx` | Home-page feature sections; presentation layer / client tier |
| `pocket_librarian.client/src/pages/MemberPage.jsx` | Member-page feature sections; presentation layer / client tier |
| `pocket_librarian.client/src/pages/StaffPage.jsx` | Staff-page feature sections; presentation layer / client tier |
| `pocket_librarian.client/src/assets/hero.png` | Frontend static asset; presentation/client tier |
| `pocket_librarian.client/src/assets/react.svg` | Frontend static asset; presentation/client tier |
| `pocket_librarian.client/src/assets/vite.svg` | Frontend static asset; presentation/client tier |
| `pocket_librarian.client/public/favicon.svg` | Browser frontend asset; presentation/client tier |
| `pocket_librarian.client/public/icons.svg` | Frontend icon asset; presentation/client tier |
| `pocket_librarian.client/public/featured-books/81IgOp0fOiL._UF1000,1000_QL80_.jpg` | Frontend book-cover asset; presentation/client tier |
| `pocket_librarian.client/public/featured-books/Berserk_Vol_1.webp` | Frontend book-cover asset; presentation/client tier |
| `pocket_librarian.client/public/featured-books/WoT01_TheEyeOfTheWorld.jpg` | Frontend book-cover asset; presentation/client tier |
| `pocket_librarian.client/public/featured-books/cvr9780671675622_9780671675622_hr.jpg` | Frontend book-cover asset; presentation/client tier |
| `pocket_librarian.client/public/featured-books/dungeon-crawler-carl-vol-1-graphic-novel-9781638493655_hr.jpg` | Frontend book-cover asset; presentation/client tier |
| `pocket_librarian.client/public/featured-books/unsouled-cover-art-by-evan-teuteberg-v0-q1f8r6d4pz881.webp` | Frontend book-cover asset; presentation/client tier |
| `pocket_librarian.client/public/under-construction.jpg` | Shared feature-placeholder image; presentation/client tier |

## BLL

| File | N-layer / N-tier role |
| --- | --- |
| `Pocket_Librarian.Server/Pocket_Librarian.Server.csproj` | ASP.NET Core server project definition and backend dependencies; server tier support |
| `Pocket_Librarian.Server/Program.cs` | API host setup and dependency registration; application/business layer / server tier |
| `Pocket_Librarian.Server/Controllers/PipelineController.cs` | HTTP API boundary for the health-check request; application layer / server tier |
| `Pocket_Librarian.Server/Controllers/WeatherForecastController.cs` | Template API controller; application layer / server tier |
| `Pocket_Librarian.Server/Models/ManualDatabaseConnectionRequest.cs` | Temporary manual database connection request model; application layer / server tier |
| `Pocket_Librarian.Server/Models/PipelineResponse.cs` | API response model crossing the server/frontend boundary; application layer / server tier |
| `Pocket_Librarian.Server/Services/PipelineService.cs` | Business-logic orchestration between controller and repository; business-logic layer / server tier |
| `Pocket_Librarian.Server/WeatherForecast.cs` | Template server model; application layer / server tier |
| `Pocket_Librarian.Server/appsettings.Development.json` | Backend development configuration; server-tier support |
| `Pocket_Librarian.Server/Properties/launchSettings.json` | Backend launch-profile configuration; server-tier support |

## DAL

| File | N-layer / N-tier role |
| --- | --- |
| `Pocket_Librarian.Server/Repositories/PipelineRepository.cs` | SQL Server connection and `dbo.Books` query; data-access layer / database-access portion of server tier |
| `Pocket_Librarian.Server/appsettings.json` | Database connection-string configuration consumed by the repository; data-access layer support |

## Documentation

| File | N-layer / N-tier role |
| --- | --- |
| `.gitattributes` | Repository file-handling configuration; supporting project material |
| `.github/CODEOWNERS` | Repository ownership and review configuration; supporting project material |
| `.gitignore` | Repository exclusion rules for generated/local files; supporting project material |
| `Pocket_Librarian.slnLaunch` | Shared Visual Studio launch configuration; supporting project material |
| `Pocket_Librarian.slnx` | Solution-level project organization; supporting project material |
| `README.md` | Project setup and operation instructions; supporting documentation |
| `README_Instructions_for_AI.md` | AI collaboration and pull-request instructions; supporting documentation |
| `PROJECT_FILE_ARCHITECTURE.md` | This file inventory and architecture mapping; supporting documentation |
| `README_Database.md` | Database diagram transcription and design notes; supporting documentation |
| `Start-Client.cmd` | Manual frontend startup helper; project-operation support |
| `Pocket_Librarian.Server/CHANGELOG.md` | Backend change history; supporting documentation |
| `Pocket_Librarian.Server/Pocket_Librarian.Server.http` | Manual API request/test file; development documentation/support |
| `pocket_librarian.client/CHANGELOG.md` | Frontend change history; supporting documentation |
| `pocket_librarian.client/Documentation/Backend Diagram.jpg` | Backend architecture diagram; supporting documentation |
| `pocket_librarian.client/Documentation/CSE-4050 Project Proposal.pdf` | Course/project proposal; supporting documentation |
| `pocket_librarian.client/Documentation/Frontend Diagram.jpg` | Frontend architecture diagram; supporting documentation |
| `pocket_librarian.client/Documentation/Pocket_Librarian_Database.png` | Database diagram used by `README_Database.md`; supporting documentation |
| `pocket_librarian.client/README.md` | Frontend project notes; supporting documentation |
